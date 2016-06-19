using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core.StateManager
{
    public class ChunkStateManagerClient : ChunkEvent, IChunkStateManager
    {
        public Chunk chunk { get; private set; }

        //! Says whether or not the chunk is visible
        public bool Visible
        {
            get { return chunk.render.batcher.IsVisible(); }
            set { chunk.render.batcher.SetVisible(value); }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

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

        public ChunkStateManagerClient(Chunk chunk)
        {
            this.chunk = chunk;
        }

        public void Init()
        {
            // Subscribe neighbors
            SubscribeNeighbors(true);
            // Request this chunk to be generated
            OnNotified(this, ChunkState.Generate);
        }

        public void Reset()
        {
            SubscribeNeighbors(false);

            m_nextState = m_nextState.Reset();
            m_pendingStates = m_pendingStates.Reset();
            m_completedStates = m_completedStates.Reset();
            m_completedStatesSafe = m_completedStates;
            m_removalRequested = false;

            m_stateExternal = ChunkStateExternal.None;

            m_genericWorkItems.Clear();
            m_genericWorkItemsLeftToProcess = 0;
            
            m_taskRunning = false;

            Visible = false;
            PossiblyVisible = false;

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
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateVertices);
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
                // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                        return;
                }

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

                // In order to save performance, we generate geometry on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(CurrStateGenerateVertices) && GenerateVertices())
                        return;
                }
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
            public readonly ChunkStateManagerClient Chunk;
            public readonly Action Action;

            public SGenericWorkItem(ChunkStateManagerClient chunk, Action action)
            {
                Chunk = chunk;
                Action = action;
            }
        }

        private static readonly ChunkState CurrStateGenericWork = ChunkState.GenericWork;
        private static readonly ChunkState NextStateGenericWork = ChunkState.Idle;

        private static void OnGenericWork(ref SGenericWorkItem item)
        {
            ChunkStateManagerClient chunk = item.Chunk;

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

        private static void OnGenericWorkDone(ChunkStateManagerClient chunk)
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

        private static void OnGenerateData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.world.terrainGen.GenerateTerrainForChunk(chunk);

            OnGenerateDataDone(stateManager);
        }

        private static void OnGenerateDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateData);
            stateManager.m_nextState = NextStateGenerateData;
            stateManager.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(ChunkStateManagerClient stateManager)
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
                            ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
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

        private static void OnLoadData(ChunkStateManagerClient stateManager)
        {
            Serialization.Serialization.LoadChunk(stateManager.chunk);

            OnLoadDataDone(stateManager);
        }

        private static void OnLoadDataDone(ChunkStateManagerClient stateManager)
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
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                        OnLoadData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Load chunk data

        #region Save chunk data

        private static readonly ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(ChunkStateManagerClient stateManager)
        {
            Serialization.Serialization.SaveChunk(stateManager.chunk);

            OnSaveDataDone(stateManager);
        }

        private static void OnSaveDataDone(ChunkStateManagerClient stateManager)
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
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                        OnSaveData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Save chunk data

        private bool SynchronizeChunk()
        {
            // 6 neighbors are necessary
            if (ListenerCount != 6)
                return false;

            // All neighbors have to have their data loaded
            foreach (var chunkEvent in Listeners)
            {
                var stateManager = (ChunkStateManagerClient)chunkEvent;
                if (!stateManager.m_completedStates.Check(ChunkState.LoadData))
                    return false;
            }

            return true;
        }

        #region Generate vertices

        private struct SGenerateVerticesWorkItem
        {
            public readonly ChunkStateManagerClient StateManager;

            public SGenerateVerticesWorkItem(ChunkStateManagerClient stateManager)
            {
                StateManager = stateManager;
            }
        }

        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices | ChunkState.BuildVerticesNow;

        private static void OnGenerateVerices(ChunkStateManagerClient stateManager)
        {
            stateManager.chunk.render.BuildMeshData();

            OnGenerateVerticesDone(stateManager);
        }

        private static void OnGenerateVerticesDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateVertices);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool GenerateVertices()
        {
            /*Assert.IsTrue(
            m_completedTasks.Check(ChunkState.LoadData),
            string.Format("[{0},{1},{2}] - GenerateVertices set sooner than LoadData completed. Pending:{3}, Completed:{4}", Pos.X, Pos.Y, Pos.Z, m_pendingTasks, m_completedTasks)
            );*/
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            if (!SynchronizeChunk())
                return true;

            bool priority = m_pendingStates.Check(ChunkState.BuildVerticesNow);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateVertices);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateVertices);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var workItem = new SGenerateVerticesWorkItem(this);

                m_taskRunning = true;
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                            OnGenerateVerices(item.StateManager);
                        },
                        workItem,
                        priority ? Globals.Watch.ElapsedTicks : long.MaxValue)
                    );
            }
            else
            {
                OnGenerateVerticesDone(this);
            }

            return true;
        }

        #endregion Generate vertices

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

        private void SubscribeNeighbors(bool subscribe)
        {
            BlockPos pos = chunk.pos;
            SubscribeTwoNeighbors(new BlockPos(pos.x + Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x - Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y + Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y - Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y, pos.z + Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y, pos.z - Env.ChunkSize), subscribe);
        }

        private void SubscribeTwoNeighbors(BlockPos neighborPos, bool subscribe)
        {
            Chunk neighbor = chunk.world.chunks.Get(neighborPos);
            if (neighbor != null)
            {
                ChunkStateManagerClient stateManager = (ChunkStateManagerClient)neighbor.stateManager;
                // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
                stateManager.Subscribe(this, ChunkState.Idle, subscribe);
                Subscribe(stateManager, ChunkState.Idle, subscribe);
            }
        }
    }
}
