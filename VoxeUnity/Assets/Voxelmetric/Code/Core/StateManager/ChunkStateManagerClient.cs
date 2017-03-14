using System;
using System.Text;
using Voxelmetric.Code.Common;
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
            get
            {
                return chunk.GeometryHandler.Batcher.Enabled;
            }
            set
            {
                chunk.GeometryHandler.Batcher.Enabled = value;
            }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;
        
        private bool m_syncEdgeBlocks;
        
        private static readonly Action<ChunkStateManagerClient> actionOnGenerateData = OnGenerateData;
        private static readonly Action<ChunkStateManagerClient> actionOnCalculateGeometryBounds = OnCalculateGeometryBounds;
        private static readonly Action<ChunkStateManagerClient> actionOnGenerateVertices = OnGenerateVertices;
        private static readonly Action<SGenerateColliderWorkItem> actionOnGenerateCollider = arg => { OnGenerateCollider(ref arg); };
        private static readonly Action<ChunkStateManagerClient> actionOnLoadData = OnLoadData;
        private static readonly Action<ChunkStateManagerClient> actionOnSaveData = OnSaveData;

        //! Flags telling us whether pool items should be returned back to the pool
        protected ChunkPoolItemState m_poolState;
        protected ITaskPoolItem m_threadPoolItem;

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

            m_syncEdgeBlocks = true;

            m_poolState = m_poolState.Reset();
            m_threadPoolItem = null;
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

        private void ReturnPoolItems()
        {
            var pools = Globals.MemPools;

            // Global.MemPools is not thread safe and were returning values to it from a different thread.
            // Therefore, each client remembers which pool it used and once the task is finished it returns
            // it back to the pool as soon as possible from the main thread

            if (m_poolState.Check(ChunkPoolItemState.GenerateColliderThreadPI))
                pools.GenerateColliderThreadPI.Push(m_threadPoolItem as ThreadPoolItem<SGenerateColliderWorkItem>);
            else if (m_poolState.Check(ChunkPoolItemState.ThreadPI))
                pools.SMThreadPI.Push(m_threadPoolItem as ThreadPoolItem<ChunkStateManagerClient>);
            else if (m_poolState.Check(ChunkPoolItemState.TaskPI))
                pools.SMTaskPI.Push(m_threadPoolItem as TaskPoolItem<ChunkStateManagerClient>);

            m_poolState = m_poolState.Reset();
            m_threadPoolItem = null;
        }

        public override void Update()
        {
            // Return processed work items back to the pool
            ReturnPoolItems();

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
                if (m_pendingStates.Check(ChunkState.CalculateBounds) && CalculateBounds())
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
                if (Visible)
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

        #region Calculate bounds
        
        private static readonly ChunkState CurrStateCalculateBounds = ChunkState.CalculateBounds;
        private static readonly ChunkState NextStateCalculateBounds = ChunkState.Idle;

        private static void OnCalculateGeometryBounds(ChunkStateManagerClient client)
        {
            client.chunk.CalculateGeometryBounds();
            OnCalculateGeometryBoundsDone(client);
        }

        private static void OnCalculateGeometryBoundsDone(ChunkStateManagerClient client)
        {
            client.m_completedStates = client.m_completedStates.Set(CurrStateCalculateBounds);
            client.m_nextState = NextStateCalculateBounds;
            client.m_taskRunning = false;
        }

        private bool CalculateBounds()
        {
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateCalculateBounds);
            m_completedStates = m_completedStates.Reset(CurrStateCalculateBounds);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks>0)
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;

                task.Set(chunk.ThreadID, actionOnCalculateGeometryBounds, this);

                m_taskRunning = true;
                WorkPoolManager.Add(task);

                return true;
            }

            // Consume info about block having been modified
            chunk.blocks.recalculateBounds = false;
            OnCalculateGeometryBoundsDone(this);
            return false;
        }

        #endregion

        #region Generate Chunk data

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.LoadData;

        private static void OnGenerateData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.world.terrainGen.GenerateTerrain(chunk);

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
            OnLoadDataDone(stateManager, false); //TODO: change to true once the network layers is implemented properly
        }

        private bool GenerateData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData | CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;
            
            if (chunk.world.networking.isServer)
            {
                // Let server generate chunk data
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;
                
                task.Set(chunk.ThreadID, actionOnGenerateData, this);

                m_taskRunning = true;
                WorkPoolManager.Add(task);
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
            bool success = Serialization.Serialization.LoadChunk(stateManager.chunk);
            OnLoadDataDone(stateManager, success);
        }

        private static void OnLoadDataDone(ChunkStateManagerClient stateManager, bool success)
        {
            // Consume info about invalidated chunk
            stateManager.chunk.blocks.recalculateBounds = false;

            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateLoadData);
            if (success)
            {
                stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkState.CalculateBounds);
                stateManager.m_nextState = NextStateLoadData;
            }
            else if (stateManager.chunk.blocks.NonEmptyBlocks > 0)
            {
                // There was an issue with loading the file. Recalculation of bounds will be necessary
                stateManager.m_nextState = ChunkState.CalculateBounds;
                stateManager.RequestState(NextStateLoadData);
            }
            
            stateManager.m_taskRunning = false;
        }

        private bool LoadData()
        {
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateLoadData | ChunkState.CalculateBounds);
            m_completedStates = m_completedStates.Reset(CurrStateLoadData | ChunkState.CalculateBounds);
            m_completedStatesSafe = m_completedStates;
            
            var task = Globals.MemPools.SMTaskPI.Pop();
            m_poolState = m_poolState.Set(ChunkPoolItemState.TaskPI);
            m_threadPoolItem = task;
            task.Set(actionOnLoadData, this);

            m_taskRunning = true;
            IOPoolManager.Add(m_threadPoolItem);

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
            
            var task = Globals.MemPools.SMTaskPI.Pop();
            m_poolState = m_poolState.Set(ChunkPoolItemState.TaskPI);
            m_threadPoolItem = task;
            task.Set(actionOnSaveData, this);

            m_taskRunning = true;
            IOPoolManager.Add(task);

            return true;
        }

        #endregion Save chunk data
        
        private bool SynchronizeNeighbors()
        {
            // 6 neighbors are necessary
            if (ListenerCount != 6)
                return false;

            // All neighbors have to have their data loaded
            for (int i = 0; i<Listeners.Length; i++)
            {
                var stateManager = (ChunkStateManagerClient)Listeners[i];
                if (!stateManager.m_completedStates.Check(ChunkState.LoadData))
                    return false;
            }

            return true;
        }

        private void OnSynchronizeEdges()
        {
            // Search for neighbors we are vertically aligned with
            for (int i = 0; i<Listeners.Length; i++)
            {
                var chunkEvent = Listeners[i];
                var stateManager = (ChunkStateManagerClient)chunkEvent;
                Chunk neighborChunk = stateManager.chunk;

                // Sync vertical neighbors
                if (neighborChunk.pos.x==chunk.pos.x &&
                    neighborChunk.pos.z==chunk.pos.z)
                {
                    // Copy the bottom layer of a neighbor chunk to the top layer of ours
                    if (neighborChunk.pos.y>chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, 0, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, -1);
                        chunk.blocks.Copy(neighborChunk.blocks, srcIndex, dstIndex, Env.ChunkSizeWithPaddingPow2);
                    }
                    // Copy the top layer of a neighbor chunk to the bottom layer of ours
                    else // if (neighborChunk.pos.y < chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkMask, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        chunk.blocks.Copy(neighborChunk.blocks, srcIndex, dstIndex, Env.ChunkSizeWithPaddingPow2);
                    }
                }

                // Sync front and back neighbors
                if (neighborChunk.pos.x==chunk.pos.x &&
                    neighborChunk.pos.y==chunk.pos.y)
                {
                    // Copy the front layer of a neighbor chunk to the back layer of ours
                    if (neighborChunk.pos.z>chunk.pos.z)
                    {
                        // Padded area - top
                        for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, -1, 0));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(x, -1, Env.ChunkSize), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, y, 0));
                                chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(x, y, Env.ChunkSize), data);
                            }
                        }

                        // Padded area - bottom
                        for (int x = -1; x < Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, 0));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, Env.ChunkSize), data);
                        }
                    }
                    // Copy the top back layer of a neighbor chunk to the front layer of ours
                    else // if (neighborChunk.pos.z < chunk.pos.z)
                    {
                        // Padded area - top
                        for (int x = -1; x < Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, -1, Env.ChunkMask));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(x, -1, -1), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, y, Env.ChunkMask));
                                chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(x, y, -1), data);
                            }
                        }

                        // Padded area - bottom
                        for (int x = -1; x < Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, Env.ChunkMask));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, -1), data);
                        }
                    }
                }

                // Sync right and left neighbors
                if (neighborChunk.pos.y==chunk.pos.y &&
                    neighborChunk.pos.z==chunk.pos.z)
                {
                    // Copy the right layer of a neighbor chunk to the left layer of ours
                    if (neighborChunk.pos.x>chunk.pos.x)
                    {
                        // Padded area - top
                        for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(0, -1, z));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, -1, z), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(0, y, z));
                                chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, y, z), data);
                            }
                        }

                        // Padded area - bottom
                        for (int z = -1; z < Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(0, Env.ChunkSize, z));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, Env.ChunkSize, z), data);
                        }
                    }
                    // Copy the left layer of a neighbor chunk to the right layer of ours
                    else // if (neighborChunk.pos.x < chunk.pos.x)
                    {
                        // Padded area - top
                        for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(Env.ChunkMask, -1, z));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(-1, -1, z), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(Env.ChunkMask, y, z));
                                chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(-1, y, z), data);
                            }
                        }

                        // Padded area - bottom
                        for (int z = -1; z < Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(Env.ChunkMask, Env.ChunkSize, z));
                            chunk.blocks.SetPadded(Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, z), data);
                        }
                    }
                }
            }
        }
        
        private bool SynchronizeEdges()
        {
            // It is only necessary to perform the sychronization once when data is generated.
            // All subsequend changes of blocks are automatically synchronized inside ChunkBlocks
            if (!m_syncEdgeBlocks)
                return true;

            // Sync edges if there's enough time
            if (!Globals.EdgeSyncBudget.HasTimeBudget)
                return false;

            m_syncEdgeBlocks = false;

            Globals.EdgeSyncBudget.StartMeasurement();
            OnSynchronizeEdges();
            Globals.EdgeSyncBudget.StopMeasurement();
            return true;
        }

        private bool SynchronizeChunk()
        {
            // 6 neighbors are necessary to be loaded
            if (!SynchronizeNeighbors())
                return false;

            // Synchronize edge data of chunks
            if (!SynchronizeEdges())
                return false;
            
            // We need to calculate our chunk's bounds if it was invalidated
            if (chunk.blocks.recalculateBounds && chunk.blocks.NonEmptyBlocks>0)
            {
                RequestState(ChunkState.CalculateBounds);
                return false;
            }

            return true;
        }

        #region Generate collider

        private static readonly ChunkState CurrStateGenerateCollider = ChunkState.BuildCollider;

        private static void OnGenerateCollider(ref SGenerateColliderWorkItem item)
        {
            ChunkStateManagerClient stateManager = item.StateManager;
            stateManager.chunk.ChunkColliderGeometryHandler.Build(item.MinX, item.MaxX, item.MinY, item.MaxY, item.MinZ, item.MaxZ);
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

            if (!SynchronizeChunk())
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateCollider);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateCollider);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var task = Globals.MemPools.GenerateColliderThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.GenerateColliderThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    chunk.ThreadID,
                    actionOnGenerateCollider,
                    new SGenerateColliderWorkItem(
                        this,
                        chunk.m_bounds.minX, chunk.m_bounds.maxX,
                        chunk.m_bounds.minY, chunk.m_bounds.maxY,
                        chunk.m_bounds.minZ, chunk.m_bounds.maxZ
                        )
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(task);

                return true;
            }

            OnGenerateColliderDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Generate vertices
        
        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices | ChunkState.BuildVerticesNow;

        private static void OnGenerateVertices(ChunkStateManagerClient client)
        {
            client.chunk.GeometryHandler.Build(0, Env.ChunkMask, 0, Env.ChunkMask, 0, Env.ChunkMask);
            OnGenerateVerticesDone(client);
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
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    chunk.ThreadID,
                    actionOnGenerateVertices,
                    this,
                    priority ? Globals.Watch.ElapsedTicks : long.MaxValue
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(task);

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
            // If chunk was generated we need to wait for other states with higher priority to finish first
            if (m_completedStates.Check(ChunkState.Generate))
            {
                if (!m_completedStates.Check(
                    // Wait until chunk is loaded
                    ChunkState.LoadData|
                    // Wait until chunk data is stored
                    ChunkState.SaveData
                    ))
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
                ChunkStateManagerClient stateManager = neighbor.stateManager;
                // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
                stateManager.Subscribe(this, ChunkState.Idle, subscribe);
                Subscribe(stateManager, ChunkState.Idle, subscribe);
            }
        }
    }
}
