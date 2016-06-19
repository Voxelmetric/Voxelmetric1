using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;

namespace Voxelmetric.Code.Core.StateManager
{
    public class ChunkStateManagerServer : ChunkEvent, IChunkStateManager
    {
        public Chunk chunk { get; private set; }
        
        //! Specifies whether there's a task running on this Chunk
        private volatile bool m_taskRunning;

        //! Next state after currently finished state
        private ChunkState m_nextState;
        //! States waiting to be processed
        private ChunkState m_pendingStates;
        //! Tasks already executed
        private ChunkState m_completedStates;
        //! Just like m_completedStates, but it is synchronized on the main thread once a check for m_taskRunning is passed
        private ChunkState m_completedStatesSafe;
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        private bool m_removalRequested;

        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;

        //! A list of generic tasks a Chunk has to perform
        private readonly List<Action> m_genericWorkItems = new List<Action>();
        //! Number of generic tasks waiting to be finished
        private int m_genericWorkItemsLeftToProcess;

        public ChunkStateManagerServer(Chunk chunk)
        {
            this.chunk = chunk;
        }

        public void Init()
        {
            // Request this chunk to be generated
            OnNotified(this, ChunkState.Generate);
        }

        public void Reset()
        {
            m_nextState = m_nextState.Reset();
            m_pendingStates = m_pendingStates.Reset();
            m_completedStates = m_completedStates.Reset();
            m_completedStatesSafe = m_completedStates;
            m_removalRequested = false;

            m_stateExternal = ChunkStateExternal.None;

            m_genericWorkItems.Clear();
            m_genericWorkItemsLeftToProcess = 0;
            
            m_taskRunning = false;

            Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("N=");
            sb.Append(m_nextState);
            sb.Append(", P=");
            sb.Append(m_pendingStates);
            sb.Append(", C=");
            sb.Append(m_completedStates);
            return sb.ToString();
        }
        
        public void RequestState(ChunkState state)
        {
            if (state==ChunkState.Remove)
            {
                if (m_removalRequested)
                    return;
                m_removalRequested = true;

                m_pendingStates = m_pendingStates.Set(ChunkState.SaveData);
                OnNotified(this, ChunkState.Remove);
            }

            m_pendingStates = m_pendingStates.Set(state);
        }

        public bool IsStateCompleted(ChunkState state)
        {
            return m_completedStatesSafe.Check(state);
        }

        public void SetMeshBuilt()
        {
        }

        public bool CanUpdate()
        {
            // Do not do any processing as long as there is any task still running
            // Note that this check is not thread-safe because this value can be changed from a different thread. However,
            // we do not care. The worst thing that can happen is that we read a value which is one frame old. So be it.
            // Thanks to being this relaxed approach we do not need any synchronization primitives at all.
            if (m_taskRunning)
                return false;

            // Synchronize the value with what we have on a different thread. It would be no big deal not having this at
            // all. However, it is technically more correct.
            m_completedStatesSafe = m_completedStates;

            // Once this Chunk is marked as finished we ignore any further requests and won't perform any updates
            return !m_completedStatesSafe.Check(ChunkState.Remove);
        }

        public void Update()
        {
            if (m_stateExternal != ChunkStateExternal.None)
            {
                // Notify everyone listening
                NotifyAll(m_stateExternal);

                m_stateExternal = ChunkStateExternal.None;
            }

            // If removal was requested before we got to generating the chunk at all we can safely mark
            // it as removed right away
            if (m_removalRequested && !m_completedStates.Check(ChunkState.Generate))
            {
                m_completedStates = m_completedStates.Set(ChunkState.Remove);
                return;
            }

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            {
                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.LoadData) && LoadData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.GenericWork) && PerformGenericWork())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.SaveData) && SaveData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.Remove) && RemoveChunk())
                    return;
            }
        }

        private void ProcessNotifyState()
        {
            if (m_nextState == ChunkState.Idle)
                return;

            OnNotified(this, m_nextState);
            m_nextState = ChunkState.Idle;
        }
        
        public bool IsSavePossible
        {
            get { return !m_removalRequested && m_completedStatesSafe.Check(ChunkState.Generate | ChunkState.LoadData); }
        }
        
        public override void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            // Enqueue the request
            m_pendingStates = m_pendingStates.Set(state);
        }

        #region Generic work

        private struct SGenericWorkItem
        {
            public readonly ChunkStateManagerServer Chunk;
            public readonly Action Action;

            public SGenericWorkItem(ChunkStateManagerServer chunk, Action action)
            {
                Chunk = chunk;
                Action = action;
            }
        }

        private static readonly ChunkState CurrStateGenericWork = ChunkState.GenericWork;
        private static readonly ChunkState NextStateGenericWork = ChunkState.Idle;

        private static void OnGenericWork(ref SGenericWorkItem item)
        {
            ChunkStateManagerServer chunk = item.Chunk;

            // Perform the action
            item.Action();

            int cnt = Interlocked.Decrement(ref chunk.m_genericWorkItemsLeftToProcess);
            if (cnt <= 0)
            {
                // Something is very wrong if we go below zero
                Assert.IsTrue(cnt == 0);

                // All generic work is done
                OnGenericWorkDone(chunk);
            }
        }

        private static void OnGenericWorkDone(ChunkStateManagerServer chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenericWork);
            chunk.m_nextState = NextStateGenericWork;
            chunk.m_taskRunning = false;
        }

        private bool PerformGenericWork()
        {
            // When we get here we expect all generic tasks to be processed
            Assert.IsTrue(Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0) == 0);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenericWork);
            m_completedStates = m_completedStates.Reset(CurrStateGenericWork);
            m_completedStatesSafe = m_completedStates;

            // If there's nothing to do we can skip this state
            if (m_genericWorkItems.Count <= 0)
            {
                m_genericWorkItemsLeftToProcess = 0;
                OnGenericWorkDone(this);
                return false;
            }

            m_taskRunning = true;
            m_genericWorkItemsLeftToProcess = m_genericWorkItems.Count;

            for (int i = 0; i < m_genericWorkItems.Count; i++)
            {
                SGenericWorkItem workItem = new SGenericWorkItem(this, m_genericWorkItems[i]);

                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenericWorkItem item = (SGenericWorkItem)arg;
                            OnGenericWork(ref item);
                        },
                        workItem)
                    );
            }
            m_genericWorkItems.Clear();

            return true;
        }

        public void EnqueueGenericTask(Action action)
        {
            Assert.IsTrue(action != null);
            m_genericWorkItems.Add(action);
            RequestState(ChunkState.GenericWork);
        }

        #endregion

        #region Generate Chunk data

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.LoadData;

        private static void OnGenerateData(ChunkStateManagerServer stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.world.terrainGen.GenerateTerrainForChunk(chunk);

            OnGenerateDataDone(stateManager);
        }

        private static void OnGenerateDataDone(ChunkStateManagerServer stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateData);
            stateManager.m_nextState = NextStateGenerateData;
            stateManager.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(ChunkStateManagerServer stateManager)
        {
            OnGenerateDataDone(stateManager);
            OnLoadDataDone(stateManager);
        }

        private bool GenerateData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData | CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;

            if (chunk.world.networking.isServer)
            {
                // Let server generate chunk data
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            ChunkStateManagerServer stateManager = (ChunkStateManagerServer)arg;
                            OnGenerateData(stateManager);
                        },
                        this)
                    );
            }
            else
            {
                // Client only asks for data
                chunk.world.networking.client.RequestChunk(chunk.pos);
            }

            return true;
        }

        #endregion Generate chunk data

        #region Load chunk data

        private static readonly ChunkState CurrStateLoadData = ChunkState.LoadData;
        private static readonly ChunkState NextStateLoadData = ChunkState.BuildVertices;

        private static void OnLoadData(ChunkStateManagerServer stateManager)
        {
            Serialization.Serialization.LoadChunk(stateManager.chunk);

            OnLoadDataDone(stateManager);
        }

        private static void OnLoadDataDone(ChunkStateManagerServer stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateLoadData);
            stateManager.m_nextState = NextStateLoadData;
            stateManager.m_taskRunning = false;
        }

        private bool LoadData()
        {
            /*Assert.IsTrue(
                m_completedStates.Check(ChunkState.Generate),
                string.Format(
                    "[{0},{1},{2}] - LoadData set sooner than Generate completed. Pending:{3}, Completed:{4}", pos.x,
                    pos.y, pos.z, m_pendingStates, m_completedStates)
                );*/
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateLoadData);
            m_completedStates = m_completedStates.Reset(CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        ChunkStateManagerServer stateManager = (ChunkStateManagerServer)arg;
                        OnLoadData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Load chunk data

        #region Save chunk data

        private static readonly ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(ChunkStateManagerServer stateManager)
        {
            Serialization.Serialization.SaveChunk(stateManager.chunk);

            OnSaveDataDone(stateManager);
        }

        private static void OnSaveDataDone(ChunkStateManagerServer stateManager)
        {
            stateManager.m_stateExternal = ChunkStateExternal.Saved;
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateSaveData);
            stateManager.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated and data finalized
            if (!m_completedStates.Check(ChunkState.Generate) || !m_completedStates.Check(ChunkState.LoadData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateSaveData);
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        ChunkStateManagerServer stateManager = (ChunkStateManagerServer)arg;
                        OnSaveData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Save chunk data

        #region Remove chunk

        private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

        private bool RemoveChunk()
        {
            // Wait until all generic tasks are processed
            if (Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0) != 0)
            {
                Assert.IsTrue(false);
                return true;
            }

            // If chunk was generated we need to wait for other states with higher priority to finish first
            if (m_completedStates.Check(ChunkState.Generate))
            {
                // LoadData need to finish first
                if (!m_completedStates.Check(ChunkState.LoadData))
                    return true;

                // Wait for serialization to finish as well
                if (!m_completedStates.Check(ChunkState.SaveData))
                    return true;

                m_pendingStates = m_pendingStates.Reset(CurrStateRemoveChunk);
            }

            m_completedStates = m_completedStates.Set(CurrStateRemoveChunk);
            return true;
        }

        #endregion Remove chunk        
    }
}
