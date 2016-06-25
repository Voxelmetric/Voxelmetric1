using System;
using System.Text;
using System.Threading;
using Assets.Voxelmetric.Code.Core.StateManager;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core.StateManager
{
    /// <summary>
    /// Handles state changes for chunks from a client's perspective.
    /// This means there chunk geometry rendering and chunk neighbors
    /// need to be taken into account.
    /// </summary>
    public class ChunkStateManagerClient : ChunkStateManager
    {
        //! Says whether or not the chunk is visible
        public bool Visible
        {
            get { return chunk.GeometryHandler.Batcher.IsEnabled(); }
            set
            {
                chunk.GeometryHandler.Batcher.Enable(value);
                chunk.ColliderGeometryHandler.Batcher.Enable(value);
            }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }
        
        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;

        

        public ChunkStateManagerClient(Chunk chunk) : base(chunk)
        {
        }

        public override void Init()
        {
            base.Init();

            // Subscribe neighbors
            SubscribeNeighbors(true);
        }

        public override void Reset()
        {
            base.Reset();

            SubscribeNeighbors(false);

            m_stateExternal = ChunkStateExternal.None;
            
            Visible = false;
            PossiblyVisible = false;
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

        public override void SetMeshBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateVertices);
        }

        public override void SetColliderBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateCollider);
        }

        public override void Update()
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

            // If there is no pending task, there is nothing for us to do
            ProcessNotifyState();
            if (m_pendingStates==0)
                return;

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            {
                // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                        return;

                    ProcessNotifyState();
                }
                
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

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.BuildCollider) && GenerateCollider())
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

        #region Generate collider

        private struct SGenerateColliderWorkItem
        {
            public readonly ChunkStateManagerClient StateManager;

            public SGenerateColliderWorkItem(ChunkStateManagerClient stateManager)
            {
                StateManager = stateManager;
            }
        }

        private static readonly ChunkState CurrStateGenerateCollider = ChunkState.BuildCollider;

        private static void OnGenerateCollider(ChunkStateManagerClient stateManager)
        {
            stateManager.chunk.ColliderGeometryHandler.Build();

            OnGenerateColliderDone(stateManager);
        }

        private static void OnGenerateColliderDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateCollider);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's collision geometry
        /// </summary>
        private bool GenerateCollider()
        {
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;
            
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateCollider);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateCollider);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var workItem = new SGenerateColliderWorkItem(this);

                m_taskRunning = true;
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenerateColliderWorkItem item = (SGenerateColliderWorkItem)arg;
                            OnGenerateCollider(item.StateManager);
                        },
                        workItem)
                    );

                return true;
            }

            OnGenerateColliderDone(this);
            return false;
        }

        #endregion Generate vertices

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
            stateManager.chunk.GeometryHandler.Build();

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

                return true;
            }

            OnGenerateVerticesDone(this);
            return false;
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
            Vector3Int pos = chunk.pos;
            SubscribeTwoNeighbors(new Vector3Int(pos.x + Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x - Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y + Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y - Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y, pos.z + Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y, pos.z - Env.ChunkSize), subscribe);
        }

        private void SubscribeTwoNeighbors(Vector3Int neighborPos, bool subscribe)
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
