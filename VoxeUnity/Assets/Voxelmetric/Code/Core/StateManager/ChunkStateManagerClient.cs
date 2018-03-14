using System;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Data_types;

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
                var batcher = chunk.GeometryHandler.Batcher;
                bool prev = batcher.Enabled;

                if (!value && prev)
                    // Chunk made invisible. We no longer need to build geometry for it
                    m_pendingStates = m_pendingStates.Reset(ChunkStates.CurrStateBuildVertices);
                else if(value && !prev)
                    // Chunk made visible. Make a request
                    m_pendingStates = m_pendingStates.Set(ChunkState.BuildVertices);

                batcher.Enabled = value;
            }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;
        
        //! If true, edges are to be synchronized with neighbor chunks
        private bool m_syncEdgeBlocks;

        //! Static shared pointers to callbacks
        private static readonly Action<ChunkStateManagerClient> actionOnLoadData = OnLoadData;
        private static readonly Action<ChunkStateManagerClient> actionOnPrepareGenerate = OnPrepareGenerate;
        private static readonly Action<ChunkStateManagerClient> actionOnGenerateData = OnGenerateData;
        private static readonly Action<ChunkStateManagerClient> actionOnPrepareSaveData = OnPrepareSaveData;
        private static readonly Action<ChunkStateManagerClient> actionOnSaveData = OnSaveData;
        private static readonly Action<ChunkStateManagerClient> actionOnBuildVertices = OnBuildVertices;
        private static readonly Action<ChunkStateManagerClient> actionOnBuildCollider = OnBuildCollider;
        
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

        public override void SetMeshBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(ChunkStates.CurrStateBuildVertices);
        }

        public override void SetColliderBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(ChunkStates.CurrStateBuildCollider);
        }

        private void ReturnPoolItems()
        {
            var pools = Globals.MemPools;

            // Global.MemPools is not thread safe and were returning values to it from a different thread.
            // Therefore, each client remembers which pool it used and once the task is finished it returns
            // it back to the pool as soon as possible from the main thread

            if (m_poolState.Check(ChunkPoolItemState.ThreadPI))
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
            
            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            ProcessNotifyState();
            if (m_pendingStates != 0)
            {
                // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    if (m_pendingStates.Check(ChunkState.LoadData) && LoadData())
                        return;

                    ProcessNotifyState();
                }

                if (m_pendingStates.Check(ChunkState.PrepareGenerate) && PrepareGenerate())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.PrepareSaveData) && PrepareSaveData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.SaveData) && SaveData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.Remove) && RemoveChunk())
                    return;

                // TODO: Having some sort of condition for colliders would be nice
                if (true)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(ChunkStates.CurrStateBuildCollider) && BuildCollider())
                        return;
                }

                // In order to save performance, we generate geometry on-demand - when the chunk can be seen
                if (Visible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(ChunkStates.CurrStateBuildVertices) && BuildVertices())
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
        
        #region Load chunk data

        private const ChunkState CurrStateLoadData = ChunkState.LoadData;
        private const ChunkState NextStateLoadData = ChunkState.PrepareGenerate;

        private static void OnLoadData(ChunkStateManagerClient stateManager)
        {
            bool success = Serialization.Serialization.Read(stateManager.m_save);
            OnLoadDataDone(stateManager, success);
        }

        private static void OnLoadDataDone(ChunkStateManagerClient stateManager, bool success)
        {
            if (success)
            {
                stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateLoadData);
                stateManager.m_nextState = NextStateLoadData;
            }
            else
            {
                stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateLoadData | ChunkState.PrepareGenerate);
                stateManager.m_nextState = ChunkState.Generate;
            }
            
            stateManager.m_taskRunning = false;
        }

        private bool LoadData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateLoadData);
            m_completedStates = m_completedStates.Reset(CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

            if (Features.UseSerialization)
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

        #region Prepare generate

        private const ChunkState CurrStatePrepareGenerate = ChunkState.PrepareGenerate;
        private const ChunkState NextStatePrepareGenerate = ChunkState.Generate;

        private static void OnPrepareGenerate(ChunkStateManagerClient stateManager)
        {
            bool success = stateManager.m_save.DoDecompression();
            OnPrepareGenerateDone(stateManager, success);
        }

        private static void OnPrepareGenerateDone(ChunkStateManagerClient stateManager, bool success)
        {
            // Consume info about invalidated chunk
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStatePrepareGenerate);

            if (success)
            {
                if (stateManager.m_save.IsDifferential)
                {
                    stateManager.m_nextState = NextStatePrepareGenerate;
                }
                else
                {
                    stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkState.Generate);
                    stateManager.m_nextState = ChunkState.BuildVertices;
                }
            }
            else
            {
                stateManager.m_nextState = NextStatePrepareGenerate;
            }

            stateManager.m_taskRunning = false;
        }

        private bool PrepareGenerate()
        {
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStatePrepareGenerate);
            m_completedStates = m_completedStates.Reset(CurrStatePrepareGenerate);
            m_completedStatesSafe = m_completedStates;

            if (Features.UseSerialization)
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;
                task.Set(chunk.ThreadID, actionOnPrepareGenerate, this);

                m_taskRunning = true;
                IOPoolManager.Add(m_threadPoolItem);

                return true;
            }

            OnPrepareGenerateDone(this, false);
            return false;
        }

        #endregion

        #region Generate Chunk data

        private const ChunkState CurrStateGenerateData = ChunkState.Generate;
        private const ChunkState NextStateGenerateData = ChunkState.BuildVertices;

        private static void OnGenerateData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.world.terrainGen.GenerateTerrain(chunk);

            // Commit serialization changes if any
            if (Features.UseSerialization)
                stateManager.m_save.CommitChanges();

            // Calculate the amount of non-empty blocks
            chunk.blocks.CalculateEmptyBlocks();

            //chunk.blocks.Compress();
            //chunk.blocks.Decompress();

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
            WorkPoolManager.Add(task, false);

            return true;
        }

        #endregion Generate chunk data

        #region Prepare save

        private const ChunkState CurrStatePrepareSaveData = ChunkState.PrepareSaveData;
        private const ChunkState NextStatePrepareSaveData = ChunkState.SaveData;

        private static void OnPrepareSaveData(ChunkStateManagerClient stateManager)
        {
            bool success = stateManager.m_save.DoCompression();
            OnPrepareSaveDataDone(stateManager, success);
        }

        private static void OnPrepareSaveDataDone(ChunkStateManagerClient stateManager, bool success)
        {
            if (Features.UseSerialization)
            {
                if (!success)
                {
                    // Free temporary memory in case of failure
                    stateManager.m_save.MarkAsProcessed();

                    // Consider SaveData completed as well
                    stateManager.m_completedStates = stateManager.m_completedStates.Set(NextStatePrepareSaveData);
                    stateManager.m_isSaveNeeded = false;
                }
                else
                    stateManager.m_nextState = NextStatePrepareSaveData;
            }

            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStatePrepareSaveData);
            stateManager.m_taskRunning = false;
        }

        private bool PrepareSaveData()
        {
            // We need to wait until chunk is generated
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStatePrepareSaveData);
            m_completedStates = m_completedStates.Reset(CurrStatePrepareSaveData);
            m_completedStatesSafe = m_completedStates;

            if (Features.UseSerialization)
            {
                m_save.ConsumeChanges();

                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;
                task.Set(chunk.ThreadID, actionOnPrepareSaveData, this);

                m_taskRunning = true;
                IOPoolManager.Add(task);

                return true;
            }

            OnPrepareSaveDataDone(this, false);
            return false;
        }

        #endregion Save chunk data

        #region Save chunk data

        private const ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(ChunkStateManagerClient stateManager)
        {
            bool success = Serialization.Serialization.Write(stateManager.m_save);
            OnSaveDataDone(stateManager, success);
        }

        private static void OnSaveDataDone(ChunkStateManagerClient stateManager, bool success)
        {
            if (Features.UseSerialization)
            {
                if (success)
                    // Notify listeners in case of success
                    stateManager.m_stateExternal = ChunkStateExternal.Saved;
                else
                {
                    // Free temporary memory in case of failure
                    stateManager.m_save.MarkAsProcessed();
                    stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkState.SaveData);
                }
            }
            
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateSaveData);
            stateManager.m_isSaveNeeded = false;
            stateManager.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated
            if (!m_completedStates.Check(ChunkState.PrepareSaveData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateSaveData);
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);
            m_completedStatesSafe = m_completedStates;

            if (Features.UseSerialization)
            {
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
            if (ListenerCount!=ListenerCountMax)
                return false;

            // All neighbors have to have their data generated
            for (int i = 0; i<Listeners.Length; i++)
            {
                var stateManager = (ChunkStateManagerClient)Listeners[i];
                if (stateManager!=null && !stateManager.m_completedStates.Check(ChunkState.Generate))
                    return false;
            }

            return true;
        }

        // A dummy chunk. Used e.g. for copying air block to padded area of chunks missing a neighbor
        private static readonly Chunk dummyChunk = new Chunk();

        private void OnSynchronizeEdges()
        {
            int chunkSize1 = chunk.SideSize-1;
            int sizePlusPadding = chunk.SideSize + Env.ChunkPadding;
            int sizeWithPadding = chunk.SideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int chunkIterXY = sizeWithPaddingPow2 - sizeWithPadding;

            // Search for neighbors we are vertically aligned with
            for (int i = 0; i<Listeners.Length; i++)
            {
                Chunk neighborChunk = dummyChunk;
                Vector3Int neighborPos;

                var chunkEvent = Listeners[i];
                if (chunkEvent!=null)
                {
                    var stateManager = (ChunkStateManagerClient)chunkEvent;
                    neighborChunk = stateManager.chunk;
                    neighborPos = neighborChunk.pos;
                }
                else
                {
                    switch ((Direction)i)
                    {
                        case Direction.up: neighborPos = chunk.pos.Add(0, Env.ChunkSize, 0); break;
                        case Direction.down: neighborPos = chunk.pos.Add(0, -Env.ChunkSize, 0); break;
                        case Direction.north: neighborPos = chunk.pos.Add(0, 0, Env.ChunkSize); break;
                        case Direction.south: neighborPos = chunk.pos.Add(0, 0, -Env.ChunkSize); break;
                        case Direction.east: neighborPos = chunk.pos.Add(Env.ChunkSize, 0, 0); break;
                        default: neighborPos = chunk.pos.Add(-Env.ChunkSize, 0, 0); break;
                    }
                }

                // Sync vertical neighbors
                if (neighborPos.x==chunk.pos.x && neighborPos.z==chunk.pos.z)
                {
                    // Copy the bottom layer of a neighbor chunk to the top layer of ours
                    if (neighborPos.y>chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, 0, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, -1);
                        chunk.blocks.Copy(neighborChunk.blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                    // Copy the top layer of a neighbor chunk to the bottom layer of ours
                    else // if (neighborPos.y < chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, chunkSize1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        chunk.blocks.Copy(neighborChunk.blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                }

                // Sync front and back neighbors
                if (neighborPos.x==chunk.pos.x && neighborPos.y==chunk.pos.y)
                {
                    // Copy the front layer of a neighbor chunk to the back layer of ours
                    if (neighborPos.z>chunk.pos.z)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, 0);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, Env.ChunkSize);
                        for (int y = -1;
                             y<sizePlusPadding;
                             y++, srcIndex += chunkIterXY, dstIndex += chunkIterXY)
                        {
                            for (int x = -1; x<sizePlusPadding; x++, srcIndex++, dstIndex++)
                            {
                                BlockData data = neighborChunk.blocks.Get(srcIndex);
                                chunk.blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                    // Copy the top back layer of a neighbor chunk to the front layer of ours
                    else // if (neighborPos.z < chunk.pos.z)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, chunkSize1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        for (int y = -1;
                             y<sizePlusPadding;
                             y++, srcIndex += chunkIterXY, dstIndex += chunkIterXY)
                        {
                            for (int x = -1; x<sizePlusPadding; x++, srcIndex++, dstIndex++)
                            {
                                BlockData data = neighborChunk.blocks.Get(srcIndex);
                                chunk.blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                }

                // Sync right and left neighbors
                if (neighborPos.y==chunk.pos.y && neighborPos.z==chunk.pos.z)
                {
                    // Copy the right layer of a neighbor chunk to the left layer of ours
                    if (neighborPos.x>chunk.pos.x)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(0, -1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, -1, -1);
                        for (int y = -1; y<sizePlusPadding; y++)
                        {
                            for (int z = -1;
                                 z<sizePlusPadding;
                                 z++, srcIndex += sizeWithPadding, dstIndex += sizeWithPadding)
                            {
                                BlockData data = neighborChunk.blocks.Get(srcIndex);
                                chunk.blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                    // Copy the left layer of a neighbor chunk to the right layer of ours
                    else // if (neighborPos.x < chunk.pos.x)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(chunkSize1, -1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        for (int y = -1; y<sizePlusPadding; y++)
                        {
                            for (int z = -1;
                                 z<sizePlusPadding;
                                 z++, srcIndex += sizeWithPadding, dstIndex += sizeWithPadding)
                            {
                                BlockData data = neighborChunk.blocks.Get(srcIndex);
                                chunk.blocks.SetRaw(dstIndex, data);
                            }
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
            // 6 neighbors are necessary to be loaded before synchronizing
            return SynchronizeNeighbors() && SynchronizeEdges();
        }

        #region Build collider geometry

        private static void OnBuildCollider(ChunkStateManagerClient client)
        {
            Chunk chunk = client.chunk;
            chunk.ChunkColliderGeometryHandler.Build();
            OnBuildColliderDone(client);
        }

        private static void OnBuildColliderDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkStates.CurrStateBuildCollider);
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
            
            bool priority = m_pendingStates.Check(ChunkState.BuildColliderNow);

            m_pendingStates = m_pendingStates.Reset(ChunkStates.CurrStateBuildCollider);
            m_completedStates = m_completedStates.Reset(ChunkStates.CurrStateBuildCollider);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    chunk.ThreadID,
                    actionOnBuildCollider,
                    this,
                    priority ? Globals.Watch.ElapsedTicks : long.MinValue
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(task, false);

                return true;
            }

            OnBuildColliderDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Build render geometry
        
        private static void OnBuildVertices(ChunkStateManagerClient client)
        {
            Chunk chunk = client.chunk;
            chunk.GeometryHandler.Build();
            OnBuildVerticesDone(client);
        }

        private static void OnBuildVerticesDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(ChunkStates.CurrStateBuildVertices);
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

            m_pendingStates = m_pendingStates.Reset(ChunkStates.CurrStateBuildVertices);
            m_completedStates = m_completedStates.Reset(ChunkStates.CurrStateBuildVertices);
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
                    priority ? Globals.Watch.ElapsedTicks : long.MinValue
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(task, priority);

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
                // Wait until chunk is generated
                if (!m_completedStates.Check(ChunkState.Generate))
                    return false;

                // Wait for save if it was requested
                if (m_isSaveNeeded)
                    return false;

                m_pendingStates = m_pendingStates.Reset(CurrStateRemoveChunk);
            }

            m_completedStates = m_completedStates.Set(CurrStateRemoveChunk);
            return true;
        }

        #endregion Remove chunk

        private static void UpdateListenersCount(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            World world = chunk.world;
            if (world==null)
                return;

            // Calculate how many listeners a chunk can have
            int maxListeners = 0;
            Vector3Int pos = chunk.pos;            
            if (world.CheckInsideWorld(pos.Add( Env.ChunkSize, 0, 0)) && (pos.x!=world.Bounds.maxX))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(-Env.ChunkSize, 0, 0)) && (pos.x!=world.Bounds.minX))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0,  Env.ChunkSize, 0)) && (pos.y!=world.Bounds.maxY))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, -Env.ChunkSize, 0)) && (pos.y!=world.Bounds.minY))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, 0,  Env.ChunkSize)) && (pos.z!=world.Bounds.maxZ))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, 0, -Env.ChunkSize)) && (pos.z!=world.Bounds.minZ))
                ++maxListeners;

            //int prevListeners = stateManager.ListenerCountMax;

            // Update max listeners and request geometry update
            stateManager.ListenerCountMax = maxListeners;
            
            // Request synchronization of edges and build geometry
            //if(prevListeners<maxListeners)
                stateManager.m_syncEdgeBlocks = true;

            // Geometry needs to be rebuild
            stateManager.RequestState(ChunkState.BuildVertices);

            // Collider might beed to be rebuild
            if(chunk.NeedsCollider)
                chunk.blocks.RequestCollider();
        }

        private void SubscribeNeighbors(bool subscribe)
        {
            Vector3Int pos = chunk.pos;
            SubscribeTwoNeighbors(pos.Add( Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(-Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(0,  Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, -Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, 0,  Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, 0, -Env.ChunkSize), subscribe);

            // Update required listener count
            UpdateListenersCount(this);
        }
        
        private void SubscribeTwoNeighbors(Vector3Int neighborPos, bool subscribe)
        {
            World world = chunk.world;
            if (world==null)
                return;

            // No chunk lookup if the neighbor positions can't be contained in the world
            //if (!world.CheckInsideWorld(neighborPos))
              //return;

            Chunk neighbor = world.chunks.Get(ref neighborPos);
            if (neighbor==null)
                return;

            ChunkStateManagerClient stateManager = neighbor.stateManager;
            // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
            if (subscribe)
            {
                stateManager.Register(this);
                Register(stateManager);
            }
            else
            {
                stateManager.Unregister(this);
                Unregister(stateManager);
            }

            // Update required listener count of the neighbor
            UpdateListenersCount(stateManager);
        }
    }
}
