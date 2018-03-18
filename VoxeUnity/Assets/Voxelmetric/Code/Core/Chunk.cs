using System;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core.Serialization;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.GeometryHandler;

namespace Voxelmetric.Code.Core
{
    public sealed class Chunk : ChunkEventSource
    {
        //! Static shared pointers to callbacks
        private static readonly Action<Chunk> actionOnLoadData = OnLoadData;
        private static readonly Action<Chunk> actionOnPrepareGenerate = OnPrepareGenerate;
        private static readonly Action<Chunk> actionOnGenerateData = OnGenerateData;
        private static readonly Action<Chunk> actionOnPrepareSaveData = OnPrepareSaveData;
        private static readonly Action<Chunk> actionOnSaveData = OnSaveData;
        private static readonly Action<Chunk> actionOnBuildVertices = OnBuildVertices;
        private static readonly Action<Chunk> actionOnBuildCollider = OnBuildCollider;

        //! ID used by memory pools to map the chunk to a given thread. Must be accessed from the main thread
        private static int s_id = 0;
        
        public World world { get; private set; }
        public ChunkBlocks Blocks { get; }
        
        public ChunkRenderGeometryHandler GeometryHandler { get; private set; }
        public ChunkColliderGeometryHandler ChunkColliderGeometryHandler { get; private set; }
        
        //! Save handler for chunk
        private readonly Save m_save;
        //! Custom update logic
        private ChunkLogic m_logic;

        //! Chunk position in world coordinates
        public Vector3Int Pos { get; private set; }

        //! Bounding box in world coordinates. It always considers a full-size chunk
        public AABB WorldBounds;

        //! List of neighbors
        public Chunk[] Neighbors { get; } = Helpers.CreateArray1D<Chunk>(6);
        //! Current number of neigbors        
        public int NeighborCount { get; private set; }
        //! Maximum possible number of neighbors given the circumstances
        public int NeighborCountMax { get; private set; }

        //! Size of chunk's side
        public int SideSize { get; } = 0;

        //! Bounding coordinates in local space. Corresponds to real geometry
        public int MinBounds, NaxBounds;
        //! Bounding coordinates in local space. Corresponds to collision geometry
        public int MinBoundsC, MaxBoundsC;

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        public int MaxPendingStructureListIndex;
        public bool NeedApplyStructure;

        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;
        //! States waiting to be processed
        private ChunkState m_pendingStates;
        //! Tasks already executed
        private ChunkState m_completedStates;
        //! Specifies whether there's a task running on this Chunk
        private volatile bool m_taskRunning;
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        private bool m_removalRequested;
        //! If true, edges are to be synchronized with neighbor chunks
        private bool m_syncEdgeBlocks;

        //! Flags telling us whether pool items should be returned back to the pool
        private ChunkPoolItemState m_poolState;
        private ITaskPoolItem m_threadPoolItem;

        //! Says whether the chunk needs its collider rebuilt
        private bool m_needsCollider;
        public bool NeedsCollider
        {
            get
            {
                return m_needsCollider;
            }
            set
            {
                bool prevNeedCollider = m_needsCollider;
                m_needsCollider = value;
                if (m_needsCollider && !prevNeedCollider)
                    Blocks.RequestCollider();
            }
        }

        //! Says whether or not the chunk is visible
        public bool Visible
        {
            get
            {
                return GeometryHandler.Batcher.Enabled;
            }
            set
            {
                var batcher = GeometryHandler.Batcher;
                bool prev = batcher.Enabled;

                if (!value && prev)
                    // Chunk made invisible. We no longer need to build geometry for it
                    ResetStatePending(ChunkStates.CurrStateBuildVertices);
                else if (value && !prev)
                    // Chunk made visible. Make a request
                    SetStatePending(ChunkState.BuildVertices);

                batcher.Enabled = value;
            }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

        public bool CanUpdate
        {
            get
            {
                // Do not do any processing as long as there is any task still running
                // Note that this check is not thread-safe because this value can be changed from a different thread. However,
                // we do not care. The worst thing that can happen is that we read a value which is one frame old...
                // Thanks to this relaxed approach we do not need any synchronization primitives anywhere.
                if (m_taskRunning)
                    return false;

                // Once this Chunk is marked as finished we ignore any further requests and won't perform any updates
                return !m_completedStates.Check(ChunkState.Remove);
            }
        }

        public bool IsSavePossible
        {
            get
            {
                // Serialization must be enabled
                if (!Features.UseSerialization)
                    return false;

                // Chunk has to be generated first before we can save it
                if (!m_completedStates.Check(ChunkState.Generate))
                    return false;

                // When doing a pure differential serialization chunk needs to be modified before we can save it
                return
                    !Features.UseDifferentialSerialization ||
                    Features.UseDifferentialSerialization_ForceSaveHeaders ||
                    Blocks.modifiedBlocks.Count > 0;
            }
        }

        public bool IsUpdateBlocksPossible
        {
            get
            {
                // Chunk has to be generated first before we can update its blocks
                if (!m_completedStates.Check(ChunkState.Generate))
                    return false;

                // Never update during saving
                return !m_pendingStates.Check(ChunkState.PrepareSaveData) && !m_pendingStates.Check(ChunkState.SaveData);
            }
        }

        /// <summary>
        /// Takes a chunk from the memory pool and intiates it
        /// </summary>
        /// <param name="world">World to which this chunk belongs</param>
        /// <param name="pos">Chunk position in world coordinates</param>
        /// <returns>A new chunk</returns>
        public static Chunk Create(World world, Vector3Int pos)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();
            chunk.Init(world, pos);
            return chunk;
        }        

        /// <summary>
        /// Returns a chunk back to the memory pool
        /// </summary>
        /// <param name="chunk">Chunk to be returned back to the memory pool</param>
        public static void Remove(Chunk chunk)
        {
            Assert.IsTrue(chunk.IsStateCompleted(ChunkState.Remove));

            // Reset the chunk back to defaults
            chunk.Reset();
            chunk.world = null;

            // Return the chunk pack to object pool
            Globals.MemPools.ChunkPool.Push(chunk);
        }

        public Chunk(int sideSize = Env.ChunkSize)
        {
            SideSize = sideSize;

            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            
            Blocks = new ChunkBlocks(this, sideSize);
            if (Features.UseSerialization)
                m_save = new Save(this);
        }

        public void Init(World world, Vector3Int pos)
        {
            this.world = world;
            Pos = pos;

            if (world!=null)
            {
                m_logic = world.config.randomUpdateFrequency>0.0f ? new ChunkLogic(this) : null;

                if (GeometryHandler==null)
                    GeometryHandler = new ChunkRenderGeometryHandler(this, world.renderMaterials);
                if (ChunkColliderGeometryHandler==null)
                    ChunkColliderGeometryHandler = new ChunkColliderGeometryHandler(this, world.physicsMaterials);
            }
            else
            {
                if (GeometryHandler == null)
                    GeometryHandler = new ChunkRenderGeometryHandler(this, null);
                if (ChunkColliderGeometryHandler == null)
                    ChunkColliderGeometryHandler = new ChunkColliderGeometryHandler(this, null);
            }

            WorldBounds = new AABB(
                pos.x, pos.y, pos.z,
                pos.x+ SideSize, pos.y+ SideSize, pos.z+ SideSize
                );

            Reset();

            Blocks.Init();

            // Request this chunk to be generated
            m_pendingStates = m_pendingStates.Set(ChunkState.LoadData);

            // Subscribe neighbors
            SubscribeNeighbors(true);
        }

        private void Reset()
        {
            // Unsubscribe neighbors
            SubscribeNeighbors(false);

            // Reset neighor data
            NeighborCount = 0;
            NeighborCountMax = 0;
            for (int i = 0; i < Neighbors.Length; i++)
                Neighbors[i] = null;

            m_stateExternal = ChunkStateExternal.None;
            m_pendingStates = m_pendingStates.Reset();
            m_completedStates = m_completedStates.Reset();

            m_poolState = m_poolState.Reset();
            m_taskRunning = false;
            m_threadPoolItem = null;

            Visible = false;
            PossiblyVisible = false;
            m_syncEdgeBlocks = true;
            m_needsCollider = false;
            m_removalRequested = false;

            NeedApplyStructure = true;
            MaxPendingStructureListIndex = 0;

            MinBounds = NaxBounds = 0;
            MinBoundsC = MaxBoundsC = 0;

            Blocks.Reset();
            if (m_logic!=null)
                m_logic.Reset();
            if (m_save != null)
                m_save.Reset();

            Clear();

            GeometryHandler.Reset();
            ChunkColliderGeometryHandler.Reset();

            //chunk.world = null; <-- must not be done inside here! Do it outside the method
        }
        
        public bool UpdateCollisionGeometry()
        {
            Profiler.BeginSample("UpdateCollisionGeometry");
            // Release the collider when no longer needed
            if (!NeedsCollider)
            {
                ResetStateCompleted(ChunkStates.CurrStateBuildCollider);
                ChunkColliderGeometryHandler.Reset();
                return false;
            }

            // Build collision geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget)
                return false;
            
            // Build collider if necessary
            if (!IsStateCompleted(ChunkStates.CurrStateBuildCollider))
                return false;
            
            Globals.GeometryBudget.StartMeasurement();

            ResetStateCompleted(ChunkStates.CurrStateBuildCollider);
            ChunkColliderGeometryHandler.Commit();

            Globals.GeometryBudget.StopMeasurement();
            Profiler.EndSample();
            return true;
        }

        public bool UpdateRenderGeometry()
        {
            Profiler.BeginSample("UpdateRenderGeometry");
            // Build render geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget)
                return false;
            
            // Build chunk mesh if necessary
            if (!IsStateCompleted(ChunkStates.CurrStateBuildVertices))
                return false;
            
            Globals.GeometryBudget.StartMeasurement();

            ResetStateCompleted(ChunkStates.CurrStateBuildVertices);
            GeometryHandler.Commit();

            Globals.GeometryBudget.StopMeasurement();
            Profiler.EndSample();
            return true;
        }

        #region Neighbors

        public bool RegisterNeighbor(Chunk neighbor)
        {
            if (neighbor == null || neighbor == this)
                return false;

            // Determine neighbors's direction as compared to current chunk
            Vector3Int p = Pos - neighbor.Pos;
            Direction dir = Direction.up;
            if (p.x < 0)
                dir = Direction.east;
            else if (p.x > 0)
                dir = Direction.west;
            else if (p.z < 0)
                dir = Direction.north;
            else if (p.z > 0)
                dir = Direction.south;
            else if (p.y > 0)
                dir = Direction.down;

            Chunk l = Neighbors[(int)dir];

            // Do not register if already registred
            if (l == neighbor)
                return false;

            // Subscribe in the first free slot
            if (l == null)
            {
                ++NeighborCount;
                Assert.IsTrue(NeighborCount <= 6);
                Neighbors[(int)dir] = neighbor;
                return true;
            }

            // We want to register but there is no free space
            Assert.IsTrue(false);

            return false;
        }

        public bool UnregisterNeighbor(Chunk neighbor)
        {
            if (neighbor == null || neighbor == this)
                return false;

            // Determine neighbors's direction as compared to current chunk
            Vector3Int p = Pos - neighbor.Pos;
            Direction dir = Direction.up;
            if (p.x < 0)
                dir = Direction.east;
            else if (p.x > 0)
                dir = Direction.west;
            else if (p.z < 0)
                dir = Direction.north;
            else if (p.z > 0)
                dir = Direction.south;
            else if (p.y > 0)
                dir = Direction.down;

            Chunk l = Neighbors[(int)dir];

            // Do not unregister if it's something else than we expected
            if (l != neighbor && l != null)
            {
                Assert.IsTrue(false);
                return false;
            }

            // Only unregister already registered sections
            if (l == neighbor)
            {
                --NeighborCount;
                Assert.IsTrue(NeighborCount >= 0);
                Neighbors[(int)dir] = null;
                return true;
            }

            return false;
        }

        private static void UpdateNeighborCount(Chunk chunk)
        {
            World world = chunk.world;
            if (world == null)
                return;

            // Calculate how many listeners a chunk can have
            int maxListeners = 0;
            Vector3Int pos = chunk.Pos;
            if (world.CheckInsideWorld(pos.Add(Env.ChunkSize, 0, 0)) && (pos.x != world.Bounds.maxX))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(-Env.ChunkSize, 0, 0)) && (pos.x != world.Bounds.minX))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, Env.ChunkSize, 0)) && (pos.y != world.Bounds.maxY))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, -Env.ChunkSize, 0)) && (pos.y != world.Bounds.minY))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, 0, Env.ChunkSize)) && (pos.z != world.Bounds.maxZ))
                ++maxListeners;
            if (world.CheckInsideWorld(pos.Add(0, 0, -Env.ChunkSize)) && (pos.z != world.Bounds.minZ))
                ++maxListeners;

            //int prevListeners = chunk.ListenerCountMax;

            // Update max listeners and request geometry update
            chunk.NeighborCountMax = maxListeners;

            // Request synchronization of edges and build geometry
            //if(prevListeners<maxListeners)
            chunk.m_syncEdgeBlocks = true;

            // Geometry needs to be rebuild
            chunk.SetStatePending(ChunkState.BuildVertices);

            // Collider might beed to be rebuild
            if (chunk.NeedsCollider)
                chunk.Blocks.RequestCollider();
        }

        private void SubscribeNeighbors(bool subscribe)
        {
            SubscribeTwoNeighbors(Pos.Add(Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(-Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, -Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, 0, Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, 0, -Env.ChunkSize), subscribe);

            // Update required listener count
            UpdateNeighborCount(this);
        }

        private void SubscribeTwoNeighbors(Vector3Int neighborPos, bool subscribe)
        {
            Chunk neighbor = world.chunks.Get(ref neighborPos);
            if (neighbor == null)
                return;

            // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
            if (subscribe)
            {
                neighbor.RegisterNeighbor(this);
                RegisterNeighbor(neighbor);
            }
            else
            {
                neighbor.UnregisterNeighbor(this);
                UnregisterNeighbor(neighbor);
            }

            // Update required listener count of the neighbor
            UpdateNeighborCount(neighbor);
        }

        #endregion

        #region Pending states

        public void SetStatePending(ChunkState state)
        {
            if (m_removalRequested && (state == ChunkState.PrepareSaveData || state == ChunkState.Remove))
                return;
            if (state == ChunkState.Remove)
            {
                m_removalRequested = true;
                if (Features.SerializeChunkWhenUnloading)
                    m_pendingStates = m_pendingStates.Set(ChunkState.PrepareSaveData);
            }

            m_pendingStates = m_pendingStates.Set(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetStatePending(ChunkState state)
        {
            m_pendingStates = m_pendingStates.Reset(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private bool IsStatePending(ChunkState state)
        {
            return m_pendingStates.Check(state);
        }

        #endregion

        #region Completed states

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void SetStateCompleted(ChunkState state)
        {
            m_completedStates = m_completedStates.Set(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void ResetStateCompleted(ChunkState state)
        {
            m_completedStates = m_completedStates.Reset(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsStateCompleted(ChunkState state)
        {
            return m_completedStates.Check(state);
        }

        #endregion

        #region State management

        public void UpdateState()
        {
            // Do not update our chunk until it has all its data prepared
            if (IsStateCompleted(ChunkState.Generate))
            {
                // Apply pending structures
                world.ApplyPendingStructures(this);

                // Update logic
                if (m_logic != null)
                    m_logic.Update();

                // Update blocks
                Blocks.Update();
            }

            // Process chunk tasks
            UpdateState_Internal();
        }

        private void UpdateState_Internal()
        {
            // Return processed work items back to the pool
            ReturnPoolItems();

            if (m_stateExternal != ChunkStateExternal.None)
            {
                // Notify everyone listening
                NotifyAll(m_stateExternal);

                m_stateExternal = ChunkStateExternal.None;
            }

            // If removal was requested before we got to loading the chunk we can safely mark
            // it as removed right away
            if (IsStatePending(ChunkState.Remove) && !IsStateCompleted(ChunkState.LoadData))
            {
                SetStateCompleted(ChunkState.Remove);
                return;
            }

            // Go from the least important bit to most important one. If a given bit is set
            // we execute a task tied with it
            {
                if (IsStatePending(ChunkState.LoadData) && LoadData())
                    return;
                if (IsStatePending(ChunkState.PrepareGenerate) && PrepareGenerate())
                    return;
                if (IsStatePending(ChunkState.Generate) && GenerateData())
                    return;
                if (IsStatePending(ChunkState.PrepareSaveData) && PrepareSaveData())
                    return;
                if (IsStatePending(ChunkState.SaveData) && SaveData())
                    return;
                if (IsStatePending(ChunkState.Remove) && RemoveChunk())
                    return;
                if (IsStatePending(ChunkStates.CurrStateBuildCollider) && BuildCollider())
                    return;
                if (IsStatePending(ChunkStates.CurrStateBuildVertices) && BuildVertices())
                    return;
            }
        }

        private void ReturnPoolItems()
        {
            var pools = Globals.MemPools;

            // Global.MemPools is not thread safe and were returning values to it from a different thread.
            // Therefore, each client remembers which pool it used and once the task is finished it returns
            // it back to the pool as soon as possible from the main thread

            if (m_poolState.Check(ChunkPoolItemState.ThreadPI))
                pools.SMThreadPI.Push(m_threadPoolItem as ThreadPoolItem<Chunk>);
            else if (m_poolState.Check(ChunkPoolItemState.TaskPI))
                pools.SMTaskPI.Push(m_threadPoolItem as TaskPoolItem<Chunk>);

            m_poolState = m_poolState.Reset();
            m_threadPoolItem = null;
        }

        #region Load chunk data

        private const ChunkState CurrStateLoadData = ChunkState.LoadData;
        private const ChunkState NextStateLoadData = ChunkState.PrepareGenerate;

        private static void OnLoadData(Chunk chunk)
        {
            bool success = Serialization.Serialization.Read(chunk.m_save);
            OnLoadDataDone(chunk, success);
        }

        private static void OnLoadDataDone(Chunk chunk, bool success)
        {
            if (success)
            {
                chunk.SetStateCompleted(CurrStateLoadData);
                chunk.SetStatePending(NextStateLoadData);
            }
            else
            {
                chunk.SetStateCompleted(CurrStateLoadData | ChunkState.PrepareGenerate);
                chunk.SetStatePending(ChunkState.Generate);
            }

            chunk.m_taskRunning = false;
        }

        private bool LoadData()
        {
            // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
            if (!PossiblyVisible)
                return true;

            ResetStatePending(CurrStateLoadData);
            ResetStateCompleted(CurrStateLoadData);

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

        private static void OnPrepareGenerate(Chunk chunk)
        {
            bool success = chunk.m_save.DoDecompression();
            OnPrepareGenerateDone(chunk, success);
        }

        private static void OnPrepareGenerateDone(Chunk chunk, bool success)
        {
            // Consume info about invalidated chunk
            chunk.SetStateCompleted(CurrStatePrepareGenerate);

            if (success)
            {
                if (chunk.m_save.IsDifferential)
                {
                    chunk.SetStatePending(NextStatePrepareGenerate);
                }
                else
                {
                    chunk.SetStateCompleted(ChunkState.Generate);
                    chunk.SetStatePending(ChunkState.BuildVertices);
                }
            }
            else
            {
                chunk.SetStatePending(NextStatePrepareGenerate);
            }

            chunk.m_taskRunning = false;
        }

        private bool PrepareGenerate()
        {
            if (!IsStateCompleted(ChunkState.LoadData))
                return true;

            ResetStatePending(CurrStatePrepareGenerate);
            ResetStateCompleted(CurrStatePrepareGenerate);

            if (Features.UseSerialization && m_save.CanDecompress())
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;
                task.Set(ThreadID, actionOnPrepareGenerate, this);

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

        private static void OnGenerateData(Chunk chunk)
        {
            chunk.world.terrainGen.GenerateTerrain(chunk);

            // Commit serialization changes if any
            if (Features.UseSerialization)
                chunk.m_save.CommitChanges();

            // Calculate the amount of non-empty blocks
            chunk.Blocks.CalculateEmptyBlocks();

            //chunk.blocks.Compress();
            //chunk.blocks.Decompress();

            OnGenerateDataDone(chunk);
        }

        private static void OnGenerateDataDone(Chunk chunk)
        {
            chunk.SetStateCompleted(CurrStateGenerateData);
            chunk.SetStatePending(NextStateGenerateData);
            chunk.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(Chunk chunk)
        {
            OnGenerateDataDone(chunk);
            OnLoadDataDone(chunk, false); //TODO: change to true once the network layers is implemented properly
        }

        private bool GenerateData()
        {
            if (!IsStateCompleted(ChunkState.LoadData))
                return true;

            ResetStatePending(CurrStateGenerateData);
            ResetStateCompleted(CurrStateGenerateData);

            var task = Globals.MemPools.SMThreadPI.Pop();
            m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
            m_threadPoolItem = task;

            task.Set(ThreadID, actionOnGenerateData, this);

            m_taskRunning = true;
            WorkPoolManager.Add(task, false);

            return true;
        }

        #endregion Generate chunk data

        #region Prepare save

        private const ChunkState CurrStatePrepareSaveData = ChunkState.PrepareSaveData;
        private const ChunkState NextStatePrepareSaveData = ChunkState.SaveData;

        private static void OnPrepareSaveData(Chunk chunk)
        {
            bool success = chunk.m_save.DoCompression();
            OnPrepareSaveDataDone(chunk, success);
        }

        private static void OnPrepareSaveDataDone(Chunk chunk, bool success)
        {
            if (Features.UseSerialization)
            {
                if (!success)
                {
                    // Free temporary memory in case of failure
                    chunk.m_save.MarkAsProcessed();

                    // Consider SaveData completed as well
                    chunk.SetStateCompleted(NextStatePrepareSaveData);
                }
                chunk.SetStatePending(NextStatePrepareSaveData);
            }

            chunk.SetStateCompleted(CurrStatePrepareSaveData);
            chunk.m_taskRunning = false;
        }

        private bool PrepareSaveData()
        {
            // We need to wait until chunk is generated
            if (!IsStateCompleted(ChunkState.Generate))
                return true;

            ResetStatePending(CurrStatePrepareSaveData);
            ResetStateCompleted(CurrStatePrepareSaveData);

            if (Features.UseSerialization && m_save.ConsumeChanges())
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;
                task.Set(ThreadID, actionOnPrepareSaveData, this);

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

        private static void OnSaveData(Chunk chunk)
        {
            bool success = Serialization.Serialization.Write(chunk.m_save);
            OnSaveDataDone(chunk, success);
        }

        private static void OnSaveDataDone(Chunk chunk, bool success)
        {
            if (Features.UseSerialization)
            {
                if (success)
                    // Notify listeners in case of success
                    chunk.m_stateExternal = ChunkStateExternal.Saved;
                // Free temporary memory in case of failure
                chunk.m_save.MarkAsProcessed();
                chunk.SetStateCompleted(ChunkState.SaveData);
            }

            chunk.SetStateCompleted(CurrStateSaveData);
            chunk.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated
            if (!IsStateCompleted(ChunkState.PrepareSaveData))
                return true;

            ResetStatePending(CurrStateSaveData);
            ResetStateCompleted(CurrStateSaveData);

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

        #region Synchronize edges

        private bool SynchronizeNeighbors()
        {
            // 6 neighbors are necessary
            if (NeighborCount != NeighborCountMax)
                return false;

            // All neighbors have to have their data generated
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = Neighbors[i];
                if (neighbor != null && !neighbor.IsStateCompleted(ChunkState.Generate))
                    return false;
            }

            return true;
        }

        // A dummy chunk. Used e.g. for copying air block to padded area of chunks missing a neighbor
        private static readonly Chunk dummyChunk = new Chunk();

        private void OnSynchronizeEdges()
        {
            int chunkSize1 = SideSize - 1;
            int sizePlusPadding = SideSize + Env.ChunkPadding;
            int sizeWithPadding = SideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int chunkIterXY = sizeWithPaddingPow2 - sizeWithPadding;

            // Search for neighbors we are vertically aligned with
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Chunk neighborChunk = dummyChunk;
                Vector3Int neighborPos;

                var neighbor = Neighbors[i];
                if (neighbor != null)
                {
                    neighborChunk = neighbor;
                    neighborPos = neighbor.Pos;
                }
                else
                {
                    switch ((Direction)i)
                    {
                        case Direction.up: neighborPos = Pos.Add(0, Env.ChunkSize, 0); break;
                        case Direction.down: neighborPos = Pos.Add(0, -Env.ChunkSize, 0); break;
                        case Direction.north: neighborPos = Pos.Add(0, 0, Env.ChunkSize); break;
                        case Direction.south: neighborPos = Pos.Add(0, 0, -Env.ChunkSize); break;
                        case Direction.east: neighborPos = Pos.Add(Env.ChunkSize, 0, 0); break;
                        default: neighborPos = Pos.Add(-Env.ChunkSize, 0, 0); break;
                    }
                }

                // Sync vertical neighbors
                if (neighborPos.x == Pos.x && neighborPos.z == Pos.z)
                {
                    // Copy the bottom layer of a neighbor chunk to the top layer of ours
                    if (neighborPos.y > Pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, 0, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, -1);
                        Blocks.Copy(neighborChunk.Blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                    // Copy the top layer of a neighbor chunk to the bottom layer of ours
                    else // if (neighborPos.y < chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, chunkSize1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        Blocks.Copy(neighborChunk.Blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                }

                // Sync front and back neighbors
                if (neighborPos.x == Pos.x && neighborPos.y == Pos.y)
                {
                    // Copy the front layer of a neighbor chunk to the back layer of ours
                    if (neighborPos.z > Pos.z)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, 0);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, Env.ChunkSize);
                        for (int y = -1;
                             y < sizePlusPadding;
                             y++, srcIndex += chunkIterXY, dstIndex += chunkIterXY)
                        {
                            for (int x = -1; x < sizePlusPadding; x++, srcIndex++, dstIndex++)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                    // Copy the top back layer of a neighbor chunk to the front layer of ours
                    else // if (neighborPos.z < chunk.pos.z)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, chunkSize1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        for (int y = -1;
                             y < sizePlusPadding;
                             y++, srcIndex += chunkIterXY, dstIndex += chunkIterXY)
                        {
                            for (int x = -1; x < sizePlusPadding; x++, srcIndex++, dstIndex++)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                }

                // Sync right and left neighbors
                if (neighborPos.y == Pos.y && neighborPos.z == Pos.z)
                {
                    // Copy the right layer of a neighbor chunk to the left layer of ours
                    if (neighborPos.x > Pos.x)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(0, -1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, -1, -1);
                        for (int y = -1; y < sizePlusPadding; y++)
                        {
                            for (int z = -1;
                                 z < sizePlusPadding;
                                 z++, srcIndex += sizeWithPadding, dstIndex += sizeWithPadding)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                    // Copy the left layer of a neighbor chunk to the right layer of ours
                    else // if (neighborPos.x < chunk.pos.x)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(chunkSize1, -1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        for (int y = -1; y < sizePlusPadding; y++)
                        {
                            for (int z = -1;
                                 z < sizePlusPadding;
                                 z++, srcIndex += sizeWithPadding, dstIndex += sizeWithPadding)
                            {
                                BlockData data = neighborChunk.Blocks.Get(srcIndex);
                                Blocks.SetRaw(dstIndex, data);
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

        #endregion

        #region Build collider geometry

        private static void OnBuildCollider(Chunk client)
        {
            client.ChunkColliderGeometryHandler.Build();
            OnBuildColliderDone(client);
        }

        private static void OnBuildColliderDone(Chunk chunk)
        {
            chunk.SetStateCompleted(ChunkStates.CurrStateBuildCollider);
            chunk.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's collision geometry
        /// </summary>
        private bool BuildCollider()
        {
            // TODO: Having some sort of condition for colliders would be nice
            //if (!xyz)
            //return true;

            if (!IsStateCompleted(ChunkState.Generate))
                return true;

            if (!SynchronizeChunk())
                return true;

            bool priority = IsStatePending(ChunkState.BuildColliderNow);

            ResetStatePending(ChunkStates.CurrStateBuildCollider);
            ResetStateCompleted(ChunkStates.CurrStateBuildCollider);

            if (Blocks.NonEmptyBlocks > 0)
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    ThreadID,
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

        private static void OnBuildVertices(Chunk client)
        {
            client.GeometryHandler.Build();
            OnBuildVerticesDone(client);
        }

        private static void OnBuildVerticesDone(Chunk chunk)
        {
            chunk.SetStateCompleted(ChunkStates.CurrStateBuildVertices);
            chunk.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool BuildVertices()
        {
            // To save performance we generate geometry on-demand - when the chunk can be seen
            if (!Visible)
                return true;

            if (!IsStateCompleted(ChunkState.Generate))
                return true;

            if (!SynchronizeChunk())
                return true;

            bool priority = IsStatePending(ChunkState.BuildVerticesNow);

            ResetStatePending(ChunkStates.CurrStateBuildVertices);
            ResetStateCompleted(ChunkStates.CurrStateBuildVertices);

            if (Blocks.NonEmptyBlocks > 0)
            {
                var task = Globals.MemPools.SMThreadPI.Pop();
                m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
                m_threadPoolItem = task;

                task.Set(
                    ThreadID,
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

        private bool RemoveChunk()
        {
            // If chunk was loaded we need to wait for other states with higher priority to finish first
            if (IsStateCompleted(ChunkState.LoadData))
            {
                // Wait until chunk is generated
                if (!IsStateCompleted(ChunkState.Generate))
                    return false;

                // Wait for save to complete if it was requested
                if (IsStatePending(ChunkState.PrepareSaveData) || IsStatePending(ChunkState.SaveData))
                    return false;

                ResetStatePending(ChunkState.Remove);
            }

            SetStateCompleted(ChunkState.Remove);
            return true;
        }

        #endregion Remove chunk

        #endregion
    }
}