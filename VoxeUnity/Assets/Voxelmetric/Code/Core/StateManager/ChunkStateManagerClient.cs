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
        
        //! If true, edges are to be synchronized with neighbor chunks
        private bool m_syncEdgeBlocks;

        //! Static shared pointers to callbacks
        private static readonly Action<ChunkStateManagerClient> actionOnGenerateData = OnGenerateData;
        private static readonly Action<ChunkStateManagerClient> actionOnCalculateGeometryBounds = OnCalculateGeometryBounds;
        private static readonly Action<ChunkStateManagerClient> actionOnBuildVertices = OnBuildVertices;
        private static readonly Action<SBuildColliderWorkItem> actionOnBuildCollider = arg => { OnBuildCollider(ref arg); };
        private static readonly Action<ChunkStateManagerClient> actionOnLoadData = OnLoadData;
        private static readonly Action<ChunkStateManagerClient> actionOnSaveData = OnSaveData;

        //! Flags telling us whether pool items should be returned back to the pool
        private ChunkPoolItemState m_poolState;
        private ITaskPoolItem m_threadPoolItem;
        
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
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateBuildVertices);
        }

        public override void SetColliderBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateBuildCollider);
        }

        private void ReturnPoolItems()
        {
            var pools = Globals.MemPools;

            // Global.MemPools is not thread safe and were returning values to it from a different thread.
            // Therefore, each client remembers which pool it used and once the task is finished it returns
            // it back to the pool as soon as possible from the main thread

            if (m_poolState.Check(ChunkPoolItemState.BuildColliderThreadPI))
                pools.BuildColliderThreadPI.Push(m_threadPoolItem as ThreadPoolItem<SBuildColliderWorkItem>);
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

            // If removal was requested before we got to loading the chunk at all we can safely mark
            // it as removed right away
            if (m_removalRequested && !m_completedStates.Check(ChunkState.LoadData))
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
                    if (m_pendingStates.Check(ChunkState.LoadData) && LoadData())
                        return;

                    ProcessNotifyState();
                }
                
                if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
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
                if (m_pendingStates.Check(ChunkState.BuildCollider) && BuildCollider())
                    return;

                // In order to save performance, we generate geometry on-demand - when the chunk can be seen
                if (Visible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(CurrStateBuildVertices) && BuildVertices())
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
            if (!m_completedStates.Check(ChunkState.Generate))
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
        private static readonly ChunkState NextStateGenerateData = ChunkState.BuildVertices;

        private static void OnGenerateData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.world.terrainGen.GenerateTerrain(chunk);

            // Commit serialization changes if any
            if (Utilities.Core.UseSerialization)
                stateManager.m_save.CommitChanges();

            // Calculate the amount of non-empty blocks
            chunk.blocks.CalculateEmptyBlocks();

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
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData);
            m_completedStatesSafe = m_completedStates;
            
            var task = Globals.MemPools.SMThreadPI.Pop();
            m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
            m_threadPoolItem = task;
                
            task.Set(chunk.ThreadID, actionOnGenerateData, this);

            m_taskRunning = true;
            WorkPoolManager.Add(task);

            return true;
        }

        #endregion Generate chunk data

        #region Load chunk data

        private static readonly ChunkState CurrStateLoadData = ChunkState.LoadData;
        private static readonly ChunkState NextStateLoadData = ChunkState.Generate;

        private static void OnLoadData(ChunkStateManagerClient stateManager)
        {
            bool success = Serialization.Serialization.Read(stateManager.m_save);
            OnLoadDataDone(stateManager, success);
        }

        private static void OnLoadDataDone(ChunkStateManagerClient stateManager, bool success)
        {
            // Consume info about invalidated chunk
            Chunk chunk = stateManager.chunk;
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateLoadData);
            
            if (success)
            {
                chunk.blocks.recalculateBounds = false;

                if (stateManager.m_save.IsDifferential)
                {
                    stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkState.CalculateBounds);
                    stateManager.m_nextState = NextStateLoadData;
                }
                else
                {
                    stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkState.CalculateBounds | ChunkState.Generate);
                    stateManager.m_nextState = ChunkState.BuildVertices;
                }
            }
            else
            {
                stateManager.m_nextState = NextStateLoadData;
            }
            
            stateManager.m_taskRunning = false;
        }

        private bool LoadData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateLoadData);
            m_completedStates = m_completedStates.Reset(CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

            if (Utilities.Core.UseSerialization)
            {
                var task = Globals.MemPools.SMTaskPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.TaskPI);
                m_threadPoolItem = task;
                task.Set(actionOnLoadData, this);

                m_taskRunning = true;
                IOPoolManager.Add(m_threadPoolItem);

                return true;
            }

            OnLoadDataDone(this, false);
            return false;
        }

        #endregion Load chunk data

        #region Save chunk data

        private static readonly ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(ChunkStateManagerClient stateManager)
        {
            bool success = Serialization.Serialization.Write(stateManager.m_save);
            OnSaveDataDone(stateManager, success);
        }

        private static void OnSaveDataDone(ChunkStateManagerClient stateManager, bool success)
        {
            if (Utilities.Core.UseSerialization)
            {
                if(success)
                    // Notify listeners in case of success
                    stateManager.m_stateExternal = ChunkStateExternal.Saved;
                else
                    // Free temporary memory in case of failure
                    stateManager.m_save.MaskAsProcessed();
            }

            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateSaveData);
            stateManager.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateSaveData);
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);
            m_completedStatesSafe = m_completedStates;

            if (Utilities.Core.UseSerialization)
            {
                m_save.ConsumeChanges();

                var task = Globals.MemPools.SMTaskPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.TaskPI);
                m_threadPoolItem = task;
                task.Set(actionOnSaveData, this);

                m_taskRunning = true;
                IOPoolManager.Add(task);

                return true;
            }

            OnSaveDataDone(this, false);
            return false;
        }

        #endregion Save chunk data
        
        private bool SynchronizeNeighbors()
        {
            // 6 neighbors are necessary
            if (ListenerCount != 6)
                return false;

            // All neighbors have to have their data generated
            for (int i = 0; i<Listeners.Length; i++)
            {
                var stateManager = (ChunkStateManagerClient)Listeners[i];
                if (!stateManager.m_completedStates.Check(ChunkState.Generate))
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
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(x, -1, Env.ChunkSize), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, y, 0));
                                chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(x, y, Env.ChunkSize), data);
                            }
                        }

                        // Padded area - bottom
                        for (int x = -1; x < Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, 0));
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, Env.ChunkSize), data);
                        }
                    }
                    // Copy the top back layer of a neighbor chunk to the front layer of ours
                    else // if (neighborChunk.pos.z < chunk.pos.z)
                    {
                        // Padded area - top
                        for (int x = -1; x < Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, -1, Env.ChunkMask));
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(x, -1, -1), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, y, Env.ChunkMask));
                                chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(x, y, -1), data);
                            }
                        }

                        // Padded area - bottom
                        for (int x = -1; x < Env.ChunkSizePlusPadding; x++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, Env.ChunkMask));
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, -1), data);
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
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, -1, z), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(0, y, z));
                                chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, y, z), data);
                            }
                        }

                        // Padded area - bottom
                        for (int z = -1; z < Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(0, Env.ChunkSize, z));
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, Env.ChunkSize, z), data);
                        }
                    }
                    // Copy the left layer of a neighbor chunk to the right layer of ours
                    else // if (neighborChunk.pos.x < chunk.pos.x)
                    {
                        // Padded area - top
                        for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(Env.ChunkMask, -1, z));
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(-1, -1, z), data);
                        }

                        // Padded area - sides
                        for (int y = 0; y<Env.ChunkSize; y++)
                        {
                            for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                            {
                                BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(Env.ChunkMask, y, z));
                                chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(-1, y, z), data);
                            }
                        }

                        // Padded area - bottom
                        for (int z = -1; z < Env.ChunkSizePlusPadding; z++)
                        {
                            BlockData data = neighborChunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(Env.ChunkMask, Env.ChunkSize, z));
                            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, z), data);
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

        #region Build collider geometry

        private static readonly ChunkState CurrStateBuildCollider = ChunkState.BuildCollider;

        private static void OnBuildCollider(ref SBuildColliderWorkItem item)
        {
            ChunkStateManagerClient stateManager = item.StateManager;
            stateManager.chunk.ChunkColliderGeometryHandler.Build(item.MinX, item.MaxX, item.MinY, item.MaxY, item.MinZ, item.MaxZ);
            OnBuildColliderDone(stateManager);
        }

        private static void OnBuildColliderDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateBuildCollider);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's collision geometry
        /// </summary>
        private bool BuildCollider()
        {
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            if (!SynchronizeChunk())
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateBuildCollider);
            m_completedStates = m_completedStates.Reset(CurrStateBuildCollider);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var task = Globals.MemPools.BuildColliderThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.BuildColliderThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    chunk.ThreadID,
                    actionOnBuildCollider,
                    new SBuildColliderWorkItem(
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

            OnBuildColliderDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Build render geometry
        
        private static readonly ChunkState CurrStateBuildVertices = ChunkState.BuildVertices | ChunkState.BuildVerticesNow;

        private static void OnBuildVertices(ChunkStateManagerClient client)
        {
            client.chunk.GeometryHandler.Build(0, Env.ChunkMask, 0, Env.ChunkMask, 0, Env.ChunkMask);
            OnBuildVerticesDone(client);
        }

        private static void OnBuildVerticesDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateBuildVertices);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool BuildVertices()
        {
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            if (!SynchronizeChunk())
                return true;

            bool priority = m_pendingStates.Check(ChunkState.BuildVerticesNow);

            m_pendingStates = m_pendingStates.Reset(CurrStateBuildVertices);
            m_completedStates = m_completedStates.Reset(CurrStateBuildVertices);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    chunk.ThreadID,
                    actionOnBuildVertices,
                    this,
                    priority ? Globals.Watch.ElapsedTicks : long.MaxValue
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(task);

                return true;
            }

            OnBuildVerticesDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Remove chunk

        private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

        private bool RemoveChunk()
        {
            // If chunk was loaded we need to wait for other states with higher priority to finish first
            if (m_completedStates.Check(ChunkState.LoadData))
            {
                if (!m_completedStates.Check(
                    // Wait until chunk is generated
                    ChunkState.Generate|
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
