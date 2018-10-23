using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Core.Serialization;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.GeometryHandler;

namespace Voxelmetric.Code.Core
{
    public partial class Chunk : ChunkEventSource
    {
        //! Static shared pointers to callbacks
        private static readonly Action<Chunk> actionOnLoadData = OnLoadData;
        private static readonly Action<Chunk> actionOnPrepareGenerate = OnPrepareGenerate;
        private static readonly Action<Chunk> actionOnGenerateData = OnGenerateData;
        private static readonly Action<Chunk> actionOnPrepareSaveData = OnPrepareSaveData;
        private static readonly Action<Chunk> actionOnSaveData = OnSaveData;
        private static readonly Action<Chunk> actionOnSyncEdges = OnSynchronizeEdges;
        private static readonly Action<Chunk> actionOnBuildVertices = OnBuildVertices;
        private static readonly Action<Chunk> actionOnBuildCollider = OnBuildCollider;

        //! ID used by memory pools to map the chunk to a given thread. Must be accessed from the main thread
        private static int s_id = 0;
        
        public World world { get; private set; }
        public ChunkBlocks Blocks { get; }
        
        public ChunkRenderGeometryHandler RenderGeometryHandler { get; private set; }
        public ChunkColliderGeometryHandler ColliderGeometryHandler { get; private set; }

        //! Queue of setBlock operations to execute
        private List<ModifyOp> m_setBlockQueue;

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
        public byte NeighborCount { get; private set; }
        //! Maximum possible number of neighbors given the circumstances
        public byte NeighborCountMax { get; private set; }

        private int m_pow = 0;
        //! Size of chunk's side
        private int m_sideSize = 0;
        public int SideSize
        {
            get { return m_sideSize; }
            set
            {
                m_sideSize = value;
                m_pow = 1 + (int)Math.Log(value, 2);
            }
        }

        private long lastUpdateTimeGeometry;
        private long lastUpdateTimeCollider;
        private int rebuildMaskGeometry;
        private int rebuildMaskCollider;

        //! Bounding coordinates in local space. Corresponds to real geometry
        public int MinBounds, MaxBounds;
        //! Bounding coordinates in local space. Corresponds to collision geometry
        public int MinBoundsC, MaxBoundsC;

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        public int MaxPendingStructureListIndex;
        public bool NeedApplyStructure;

        //! State to notify event listeners about
        private ChunkStateExternal m_stateExternal;
        //! States waiting to be processed
        private ChunkState m_pendingStates;
        //! Tasks already executed
        private ChunkState m_completedStates;
        //! Specifies whether there's a task running on this Chunk
        private bool m_taskRunning;
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        private bool m_removalRequested;
        //! If true, edge synchronization is in progress
        private bool m_isSyncingEdges;

        //! Flags telling us whether pool items should be returned back to the pool
        private ChunkPoolItemState m_poolState;
        private ITaskPoolItem m_threadPoolItem;

        //! Says whether the chunk needs collision geometry
        public bool NeedsColliderGeometry
        {
            get
            {
                return ColliderGeometryHandler.Batcher.Enabled;
            }
            set
            {
                ColliderGeometryHandler.Batcher.Enabled = value;
            }
        }

        //! Says whether the chunk needs render geometry
        public bool NeedsRenderGeometry
        {
            get
            {
                return RenderGeometryHandler.Batcher.Enabled;
            }
            set
            {
                RenderGeometryHandler.Batcher.Enabled = value;
            }
        }
        
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }
        
        public bool IsSavePossible
        {
            get
            {
                // Serialization must be enabled
                if (!Features.UseSerialization)
                    return false;

                // Chunk has to be generated first before we can save it
                if ((m_completedStates&ChunkState.Generate)==0)
                    return false;

                // When doing a pure differential serialization chunk needs to be modified before we can save it
                return
                    !Features.UseDifferentialSerialization ||
                    Features.UseDifferentialSerialization_ForceSaveHeaders ||
                    Blocks.modifiedBlocks.Count > 0;
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
            Assert.IsTrue((chunk.m_completedStates&ChunkState.Remove)!=0);

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

                if (RenderGeometryHandler==null)
                    RenderGeometryHandler = new ChunkRenderGeometryHandler(this, world.renderMaterials);
                if (ColliderGeometryHandler==null)
                    ColliderGeometryHandler = new ChunkColliderGeometryHandler(this, world.physicsMaterials);
            }
            else
            {
                if (RenderGeometryHandler == null)
                    RenderGeometryHandler = new ChunkRenderGeometryHandler(this, null);
                if (ColliderGeometryHandler == null)
                    ColliderGeometryHandler = new ChunkColliderGeometryHandler(this, null);
            }

            WorldBounds = new AABB(
                pos.x, pos.y, pos.z,
                pos.x+ SideSize, pos.y+ SideSize, pos.z+ SideSize
                );

            m_setBlockQueue = new List<ModifyOp>();

            Reset();

            Blocks.Init();

            // Request this chunk to be generated
            m_pendingStates |= ChunkState.LoadData;

            // Subscribe neighbors
            SubscribeNeighbors(true);
        }

        private void Reset()
        {
            if (NeighborCountMax != 0)
            {
                // Unsubscribe neighbors
                SubscribeNeighbors(false);

                // Reset neighbor data
                NeighborCount = 0;
                NeighborCountMax = 0;
            }

            for (int i = 0; i < Neighbors.Length; i++)
                Neighbors[i] = null;

            m_stateExternal = ChunkStateExternal.None;
            m_pendingStates = ChunkState.None;
            m_completedStates = ChunkState.None;

            m_poolState = m_poolState.Reset();
            m_taskRunning = false;
            m_threadPoolItem = null;

            NeedsRenderGeometry = false;
            NeedsColliderGeometry = false;
            PossiblyVisible = false;
            m_removalRequested = false;
            m_isSyncingEdges = false;

            NeedApplyStructure = true;
            MaxPendingStructureListIndex = 0;

            MinBounds = MaxBounds = 0;
            MinBoundsC = MaxBoundsC = 0;

            lastUpdateTimeGeometry = 0;
            lastUpdateTimeCollider = 0;
            rebuildMaskGeometry = -1;
            rebuildMaskCollider = -1;

            Blocks.Reset();
            if (m_logic!=null)
                m_logic.Reset();
            if (m_save != null)
                m_save.Reset();

            Clear();

            RenderGeometryHandler.Reset();
            ColliderGeometryHandler.Reset();

            //chunk.world = null; <-- must not be done inside here! Do it outside the method
        }

        public bool UpdateCollisionGeometry()
        {
            Profiler.BeginSample("UpdateCollisionGeometry");

            // Build collision geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget) {
                Profiler.EndSample();
                return false;
            }

            // Build collider only if necessary
            if ((m_completedStates & ChunkStates.CurrStateBuildCollider) == 0) {
                Profiler.EndSample();
                return false;
            }
            
            Globals.GeometryBudget.StartMeasurement();
            {
                ColliderGeometryHandler.Commit();
                m_completedStates &= ~ChunkStates.CurrStateBuildCollider;
            }
            Globals.GeometryBudget.StopMeasurement();

            Profiler.EndSample();
            return true;
        }

        public bool UpdateRenderGeometry()
        {
            Profiler.BeginSample("UpdateRenderGeometry");

            // Build render geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget) {
                Profiler.EndSample();
                return false;
            }
            
            // Build chunk mesh only if necessary
            if ((m_completedStates&ChunkStates.CurrStateBuildVertices)==0) {
                Profiler.EndSample();
                return false;
            }
            
            Globals.GeometryBudget.StartMeasurement();
            {
                RenderGeometryHandler.Commit();
                m_completedStates &= ~ChunkStates.CurrStateBuildVertices;
            }
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

            // Calculate how many neighbors a chunk can have
            byte maxNeighbors = 0;
            Vector3Int pos = chunk.Pos;
            if (world.CheckInsideWorld(pos.Add(Env.ChunkSize, 0, 0)))
                ++maxNeighbors;
            if (world.CheckInsideWorld(pos.Add(-Env.ChunkSize, 0, 0)))
                ++maxNeighbors;
            if (world.CheckInsideWorld(pos.Add(0, Env.ChunkSize, 0)))
                ++maxNeighbors;
            if (world.CheckInsideWorld(pos.Add(0, -Env.ChunkSize, 0)))
                ++maxNeighbors;
            if (world.CheckInsideWorld(pos.Add(0, 0, Env.ChunkSize)))
                ++maxNeighbors;
            if (world.CheckInsideWorld(pos.Add(0, 0, -Env.ChunkSize)))
                ++maxNeighbors;

            // Update max neighbor count and request geometry update
            chunk.NeighborCountMax = maxNeighbors;
            
            // Geometry & collider needs to be rebuilt
            // This does not mean they will be built because the chunk might not
            // be visible or colliders might be turned off
            chunk.m_pendingStates|=ChunkState.SyncEdges;
            chunk.m_pendingStates|=ChunkState.BuildVertices;
            chunk.m_pendingStates|=ChunkState.BuildCollider;
        }

        private void SubscribeNeighbors(bool subscribe)
        {
            SubscribeTwoNeighbors(Pos.Add(Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(-Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, -Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, 0, Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(Pos.Add(0, 0, -Env.ChunkSize), subscribe);

            // Update required neighbor count
            UpdateNeighborCount(this);
        }

        private void SubscribeTwoNeighbors(Vector3Int neighborPos, bool subscribe)
        {
            Chunk neighbor = world.GetChunk(ref neighborPos);
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

            // Update required neighbor count of the neighbor
            UpdateNeighborCount(neighbor);
        }

        public bool NeedToHandleNeighbors(ref Vector3Int pos)
        {
            return rebuildMaskGeometry != 0x3f &&
                   // Only check neighbors when it is a change of a block on a chunk's edge
                   (pos.x <= 0 || pos.x >= m_sideSize - 1 ||
                    pos.y <= 0 || pos.y >= m_sideSize - 1 ||
                    pos.z <= 0 || pos.z >= m_sideSize - 1);
        }

        private ChunkBlocks HandleNeighborRight(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.east);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
                return null;

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lz != cz)
                return null;

            if (pos.x != m_sideSize - 1 || lx - m_sideSize != cx)
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborLeft(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.west);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
                return null;
            
            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lz != cz)
                return null;

            if (pos.x != 0 || lx + m_sideSize != cx)
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborUp(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.up);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            Chunk neighbor = Neighbors[i];
            if (neighbor == null)
                return null;

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (lx != cx && lz != cz)
                return null;

            if (pos.y != m_sideSize - 1 || ly - m_sideSize != cy)
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborDown(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.down);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var neighbor = Neighbors[i];
            if (neighbor == null)
                return null;

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (lx != cx && lz != cz)
                return null;

            if (pos.y != 0 || ly + m_sideSize != cy)
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborFront(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.north);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var neighbor = Neighbors[i];
            if (neighbor == null)
                return null;

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lx != cx)
                return null;

            if (pos.z != m_sideSize - 1 || lz - m_sideSize != cz)
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return neighbor.Blocks;
        }

        private ChunkBlocks HandleNeighborBack(ref Vector3Int pos)
        {
            int i = DirectionUtils.Get(Direction.south);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var neighbor = Neighbors[i];
            if (neighbor == null)
                return null;

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;
            int lx = neighbor.Pos.x;
            int ly = neighbor.Pos.y;
            int lz = neighbor.Pos.z;

            if (ly != cy && lx != cx)
                return null;

            if (pos.z != 0 || lz + m_sideSize != cz)
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return neighbor.Blocks;
        }

        public ChunkBlocks HandleNeighbor(ref Vector3Int pos, Direction dir)
        {
            switch (dir)
            {
                case Direction.up:
                    return HandleNeighborUp(ref pos);
                case Direction.down:
                    return HandleNeighborDown(ref pos);
                case Direction.north:
                    return HandleNeighborFront(ref pos);
                case Direction.south:
                    return HandleNeighborBack(ref pos);
                case Direction.east:
                    return HandleNeighborRight(ref pos);
                default: //Direction.west
                    return HandleNeighborLeft(ref pos);
            }
        }

        public void HandleNeighbors(BlockData block, Vector3Int pos)
        {
            if (!NeedToHandleNeighbors(ref pos))
                return;

            int cx = Pos.x;
            int cy = Pos.y;
            int cz = Pos.z;

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild their geometry
            for (int i = 0; i < Neighbors.Length; i++)
            {
                Chunk neighbor = Neighbors[i];
                if (neighbor == null)
                    continue;

                ChunkBlocks neighborChunkBlocks = neighbor.Blocks;

                int lx = neighbor.Pos.x;
                int ly = neighbor.Pos.y;
                int lz = neighbor.Pos.z;

                if (ly == cy || lz == cz)
                {
                    // Section to the left
                    if (pos.x == 0 && lx + m_sideSize == cx)
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(m_sideSize, pos.y, pos.z, m_pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                    // Section to the right
                    else if (pos.x == m_sideSize - 1 && lx - m_sideSize == cx)
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(-1, pos.y, pos.z, m_pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                }

                if (lx == cx || lz == cz)
                {
                    // Section to the bottom
                    if (pos.y == 0 && ly + m_sideSize == cy)
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, m_sideSize, pos.z, m_pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                    // Section to the top
                    else if (pos.y == m_sideSize - 1 && ly - m_sideSize == cy)
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, -1, pos.z, m_pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                }

                if (ly == cy || lx == cx)
                {
                    // Section to the back
                    if (pos.z == 0 && lz + m_sideSize == cz)
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, m_sideSize, m_pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                    // Section to the front
                    else if (pos.z == m_sideSize - 1 && lz - m_sideSize == cz)
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, -1, m_pow);
                        neighborChunkBlocks.SetRaw(neighborIndex, block);
                    }
                }

                // No further checks needed once we know all neighbors need to be notified
                if (rebuildMaskGeometry == 0x3f)
                    break;
            }
        }

        #endregion

        #region States management

        public void RequestRemoval()
        {
            if (m_removalRequested)
                return;
            m_removalRequested = true;
            m_pendingStates |= ChunkState.Remove;
            if (Features.SerializeChunkWhenUnloading)
                m_pendingStates |= ChunkState.PrepareSaveData;
        }

        public void RequestSave()
        {
            if (m_removalRequested)
                return;
            m_pendingStates |= ChunkState.PrepareSaveData;
        }
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsStateCompleted(ChunkState state)
        {
            return (m_completedStates&state)!=0;
        }

        #endregion

        #region State management

        public bool Update()
        {
            // Do not do any processing as long as there is any task still running
            // Note that this check is not thread-safe because this value can be changed from a different thread. However,
            // we do not care. The worst thing that can happen is that we read a value which is one frame old...
            // Thanks to this relaxed approach we do not need any synchronization primitives anywhere.
            if (m_taskRunning)
                return false;

            // Once this chunk is marked as removed we ignore any further requests and won't perform any updates
            if ((m_completedStates&ChunkState.Remove)!=0)
                return false;

            // Some operations can only be performed on a generated chunk
            if ((m_completedStates&ChunkState.Generate)!=0)
            {
                // Apply pending structures
                world.ApplyPendingStructures(this);

                // Update logic
                if (m_logic != null)
                    m_logic.Update();

                // Update blocks
                UpdateBlocks();
            }

            // Process chunk tasks
            UpdateState();

            return true;
        }

        public void UpdateBlocks()
        {
            // Chunk has to be generated first before we can update its blocks
            if ((m_completedStates&ChunkState.Generate)==0)
                return;

            // Don't update during saving
            if (
                (m_pendingStates&ChunkState.PrepareSaveData)!=0 ||
                (m_pendingStates&ChunkState.SaveData)!=0
                )
                return;
            
            // Don't update when neighbors are syncing
            // TODO: We should be interested only in edge-blocks in this case
            if (AreNeighborsSynchronizing())
                return;

            //UnityEngine.Debug.Log(m_setBlockQueue.Count);

            if (m_setBlockQueue.Count > 0)
            {
                if (rebuildMaskGeometry < 0)
                    rebuildMaskGeometry = 0;
                if (rebuildMaskCollider < 0)
                    rebuildMaskCollider = 0;

                var timeBudget = Globals.SetBlockBudget;

                // Modify blocks
                int j;
                for (j = 0; j < m_setBlockQueue.Count; j++)
                {
                    timeBudget.StartMeasurement();
                    m_setBlockQueue[j].Apply(this);
                    timeBudget.StopMeasurement();

                    // Sync edges if there's enough time
                    /*if (!timeBudget.HasTimeBudget)
                    {
                        ++j;
                        break;
                    }*/
                }

                rebuildMaskCollider |= rebuildMaskGeometry;

                if (j == m_setBlockQueue.Count)
                    m_setBlockQueue.Clear();
                else
                {
                    m_setBlockQueue.RemoveRange(0, j);
                    return;
                }
            }

            long now = Globals.Watch.ElapsedMilliseconds;

            // Request a geometry update at most 10 times a second
            if (rebuildMaskGeometry >= 0 && now - lastUpdateTimeGeometry >= 100)
            {
                lastUpdateTimeGeometry = now;

                // Request rebuild on this chunk
                m_pendingStates|=ChunkState.BuildVerticesNow;

                // Notify neighbors that they need to rebuild their geometry
                if (rebuildMaskGeometry > 0)
                {
                    for (int j = 0; j < Neighbors.Length; j++)
                    {
                        Chunk neighbor = Neighbors[j];
                        if (neighbor != null && ((rebuildMaskGeometry >> j) & 1) != 0)
                        {
                            // Request rebuild on neighbor chunks
                            neighbor.m_pendingStates|=ChunkState.BuildVerticesNow;
                        }
                    }
                }

                rebuildMaskGeometry = -1;
            }

            // Request a collider update at most 4 times a second
            if (NeedsColliderGeometry && rebuildMaskCollider >= 0 && now - lastUpdateTimeCollider >= 250)
            {
                lastUpdateTimeCollider = now;

                // Request rebuild on this chunk
                m_pendingStates|=ChunkState.BuildColliderNow;

                // Notify neighbors that they need to rebuilt their geometry
                if (rebuildMaskCollider > 0)
                {
                    for (int j = 0; j < Neighbors.Length; j++)
                    {
                        Chunk neighbor = Neighbors[j];
                        if (neighbor != null && ((rebuildMaskCollider >> j) & 1) != 0)
                        {
                            // Request rebuild on neighbor chunks
                            if (neighbor.NeedsColliderGeometry)
                                neighbor.m_pendingStates|=ChunkState.BuildColliderNow;
                        }
                    }
                }

                rebuildMaskCollider = -1;
            }
        }

        private void UpdateState()
        {
            // Return processed work items back to the pool
            ReturnPoolItems();

            if (m_stateExternal!=ChunkStateExternal.None)
            {
                // Notify everyone listening
                NotifyAll(m_stateExternal);

                m_stateExternal = ChunkStateExternal.None;
            }

            // If removal was requested before we got to loading the chunk we can safely mark
            // it as removed right away
            if ((m_pendingStates&ChunkState.Remove)!=0 && (m_completedStates&ChunkState.LoadData)==0)
            {
                m_completedStates |= ChunkState.Remove;
                return;
            }

            // Go from the least important bit to most important one. If a given bit is set
            // we execute a task tied with it
            if ((m_pendingStates&ChunkState.LoadData)!=0 && LoadData())
                return;
            if ((m_pendingStates&ChunkState.PrepareGenerate)!=0 && PrepareGenerate())
                return;
            if ((m_pendingStates&ChunkState.Generate)!=0 && GenerateData())
                return;
            if ((m_pendingStates&ChunkState.PrepareSaveData)!=0 && PrepareSaveData())
                return;
            if ((m_pendingStates&ChunkState.SaveData)!=0 && SaveData())
                return;
            if ((m_pendingStates&ChunkState.Remove)!=0 && RemoveChunk())
                return;
            if ((m_pendingStates&ChunkState.SyncEdges)!=0 && SynchronizeEdges())
                return;
            if ((m_pendingStates&ChunkStates.CurrStateBuildCollider)!=0 && BuildCollider())
                return;
            if ((m_pendingStates&ChunkStates.CurrStateBuildVertices)!=0 && BuildVertices())
                return;
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

        /// <summary>
        /// Queues a modification of blocks in a given range
        /// </summary>
        /// <param name="op">Set operation to be performed</param>
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public void Modify(ModifyOp op)
        {
            m_setBlockQueue.Add(op);
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
                chunk.m_completedStates |= CurrStateLoadData;
                chunk.m_pendingStates|=NextStateLoadData;
            }
            else
            {
                chunk.m_completedStates |= CurrStateLoadData | ChunkState.PrepareGenerate;
                chunk.m_pendingStates|=ChunkState.Generate;
            }

            chunk.m_taskRunning = false;
        }

        private bool LoadData()
        {
            // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
            if (!PossiblyVisible)
                return true;

            m_pendingStates &= ~CurrStateLoadData;
            m_completedStates &= ~CurrStateLoadData;

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
            chunk.m_completedStates |= CurrStatePrepareGenerate;

            if (success)
            {
                if (chunk.m_save.IsDifferential)
                {
                    chunk.m_pendingStates|=NextStatePrepareGenerate;
                }
                else
                {
                    chunk.m_completedStates |= ChunkState.Generate;
                    chunk.m_pendingStates|=ChunkState.BuildVertices;
                }
            }
            else
            {
                chunk.m_pendingStates|=NextStatePrepareGenerate;
            }

            chunk.m_taskRunning = false;
        }

        private bool PrepareGenerate()
        {
            if ((m_completedStates&ChunkState.LoadData)==0)
                return true;

            m_pendingStates &= ~CurrStatePrepareGenerate;
            m_completedStates &= ~CurrStatePrepareGenerate;

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
            chunk.m_completedStates |= CurrStateGenerateData;
            chunk.m_pendingStates|=NextStateGenerateData;
            chunk.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(Chunk chunk)
        {
            OnGenerateDataDone(chunk);
            OnLoadDataDone(chunk, false); //TODO: change to true once the network layers is implemented properly
        }

        private bool GenerateData()
        {
            if ((m_completedStates&ChunkState.LoadData)==0)
                return true;

            m_pendingStates &= ~CurrStateGenerateData;
            m_completedStates &= ~CurrStateGenerateData;

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
                    chunk.m_completedStates |= NextStatePrepareSaveData;
                }
                chunk.m_pendingStates|=NextStatePrepareSaveData;
            }

            chunk.m_completedStates |= CurrStatePrepareSaveData;
            chunk.m_taskRunning = false;
        }

        private bool PrepareSaveData()
        {
            // We need to wait until chunk is generated
            if ((m_completedStates&ChunkState.Generate)==0)
                return true;

            m_pendingStates &= ~CurrStatePrepareSaveData;
            m_completedStates &= ~CurrStatePrepareSaveData;

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
                // Notify listeners in case of success
                if (success)
                    chunk.m_stateExternal = ChunkStateExternal.Saved;
                // Free temporary memory in case of failure
                chunk.m_save.MarkAsProcessed();
                chunk.m_completedStates |= ChunkState.SaveData;
            }

            chunk.m_completedStates |= CurrStateSaveData;
            chunk.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated
            if ((m_completedStates&ChunkState.PrepareSaveData)==0)
                return true;

            m_pendingStates &= ~CurrStateSaveData;
            m_completedStates &= ~CurrStateSaveData;

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
        
        private bool AreNeighborsSynchronizing()
        {
            // There's has to be enough neighbors
            if (NeighborCount != NeighborCountMax)
                return false;

            // All neighbors have to have their data generated
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = Neighbors[i];
                if (neighbor!=null && neighbor.m_isSyncingEdges)
                    return true;
            }

            return false;
        }

        private bool AreNeighborsSynchronized()
        {
            // There's has to be enough neighbors
            if (NeighborCount != NeighborCountMax)
                return false;
            
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = Neighbors[i];
                if (neighbor != null && (neighbor.m_completedStates&ChunkState.SyncEdges)==0)
                    return false;
            }

            return true;
        }

        private bool AreNeighborsGenerated()
        {
            // There's has to be enough neighbors
            if (NeighborCount != NeighborCountMax)
                return false;

            // All neighbors have to have their data generated
            for (int i = 0; i < Neighbors.Length; i++)
            {
                var neighbor = Neighbors[i];
                if (neighbor != null && (neighbor.m_completedStates&ChunkState.Generate)==0)
                    return false;
            }

            return true;
        }

        // A dummy chunk. Used e.g. for copying air block to padded area of chunks missing a neighbor
        private static readonly Chunk dummyChunk = new Chunk();

        private static void OnSynchronizeEdges(Chunk chunk)
        {
            int chunkSize1 = chunk.SideSize - 1;
            int sizePlusPadding = chunk.SideSize + Env.ChunkPadding;
            int sizeWithPadding = chunk.SideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int chunkIterXY = sizeWithPaddingPow2 - sizeWithPadding;

            for (int i = 0; i < chunk.Neighbors.Length; i++)
            {
                Chunk neighborChunk = dummyChunk;
                Vector3Int neighborPos;

                var neighbor = chunk.Neighbors[i];
                if (neighbor != null)
                {
                    neighborChunk = neighbor;
                    neighborPos = neighbor.Pos;
                }
                else
                {
                    switch ((Direction)i)
                    {
                        case Direction.up: neighborPos = chunk.Pos.Add(0, Env.ChunkSize, 0); break;
                        case Direction.down: neighborPos = chunk.Pos.Add(0, -Env.ChunkSize, 0); break;
                        case Direction.north: neighborPos = chunk.Pos.Add(0, 0, Env.ChunkSize); break;
                        case Direction.south: neighborPos = chunk.Pos.Add(0, 0, -Env.ChunkSize); break;
                        case Direction.east: neighborPos = chunk.Pos.Add(Env.ChunkSize, 0, 0); break;
                        default: neighborPos = chunk.Pos.Add(-Env.ChunkSize, 0, 0); break;
                    }
                }

                // Sync vertical neighbors
                if (neighborPos.x == chunk.Pos.x && neighborPos.z == chunk.Pos.z)
                {
                    // Copy the bottom layer of a neighbor chunk to the top layer of ours
                    if (neighborPos.y > chunk.Pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, 0, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, -1);
                        chunk.Blocks.Copy(neighborChunk.Blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                    // Copy the top layer of a neighbor chunk to the bottom layer of ours
                    else // if (neighborPos.y < chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, chunkSize1, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        chunk.Blocks.Copy(neighborChunk.Blocks, srcIndex, dstIndex, sizeWithPaddingPow2);
                    }
                }

                // Sync front and back neighbors
                if (neighborPos.x == chunk.Pos.x && neighborPos.y == chunk.Pos.y)
                {
                    // Copy the front layer of a neighbor chunk to the back layer of ours
                    if (neighborPos.z > chunk.Pos.z)
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
                                chunk.Blocks.SetRaw(dstIndex, data);
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
                                chunk.Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                }

                // Sync right and left neighbors
                if (neighborPos.y == chunk.Pos.y && neighborPos.z == chunk.Pos.z)
                {
                    // Copy the right layer of a neighbor chunk to the left layer of ours
                    if (neighborPos.x > chunk.Pos.x)
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
                                chunk.Blocks.SetRaw(dstIndex, data);
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
                                chunk.Blocks.SetRaw(dstIndex, data);
                            }
                        }
                    }
                }
            }

            OnSynchronizeEdgesDone(chunk);
        }

        private static void OnSynchronizeEdgesDone(Chunk chunk)
        {
            chunk.m_completedStates |= ChunkState.SyncEdges;
            chunk.m_isSyncingEdges = false;
            chunk.m_taskRunning = false;
        }

        private bool SynchronizeEdges()
        {
            // Block while we're waiting for data to be generated
            if ((m_completedStates&ChunkState.Generate)==0)
                return true;

            // Make sure all neighbors are generated
            if (!AreNeighborsGenerated())
                return true;

            m_pendingStates &= ~ChunkState.SyncEdges;
            m_completedStates &= ~ChunkState.SyncEdges;

            var task = Globals.MemPools.SMThreadPI.Pop();
            m_poolState = m_poolState.Set(ChunkPoolItemState.ThreadPI);
            m_threadPoolItem = task;

            task.Set(ThreadID, actionOnSyncEdges, this);

            m_taskRunning = true;
            m_isSyncingEdges = true;
            WorkPoolManager.Add(task, false);
            return true;
        }

        #endregion

        #region Build collider geometry

        private static void OnBuildCollider(Chunk client)
        {
            client.ColliderGeometryHandler.Build();
            OnBuildColliderDone(client);
        }

        private static void OnBuildColliderDone(Chunk chunk)
        {
            chunk.m_completedStates |= ChunkStates.CurrStateBuildCollider;
            chunk.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's collision geometry
        /// </summary>
        private bool BuildCollider()
        {
            // To save performance we generate collider on-demand
            if (!NeedsColliderGeometry)
                return false; // Try the next step - build render geometry

            // Block while we're waiting for data to be synchronized
            if ((m_completedStates&ChunkState.SyncEdges)==0)
                return true;

            // Make sure all neighbors are synchronized
            if (!AreNeighborsSynchronized())
                return true;

            if (Blocks.NonEmptyBlocks > 0)
            {
                bool priority = (m_pendingStates&ChunkState.BuildColliderNow)!=0;

                m_pendingStates &= ~ChunkStates.CurrStateBuildCollider;
                m_completedStates &= ~ChunkStates.CurrStateBuildCollider;
            
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
            client.RenderGeometryHandler.Build();
            OnBuildVerticesDone(client);
        }

        private static void OnBuildVerticesDone(Chunk chunk)
        {
            chunk.m_completedStates |= ChunkStates.CurrStateBuildVertices;
            chunk.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool BuildVertices()
        {
            // To save performance we generate geometry on-demand - when the chunk can be seen
            if (!NeedsRenderGeometry)
                return false; // Try the next step - there's no next step :)

            // Block while we're waiting for data to be synchronized
            if ((m_completedStates&ChunkState.SyncEdges)==0)
                return true;

            // Make sure all neighbors are synchronized
            if (!AreNeighborsSynchronized())
                return true;

            if (Blocks.NonEmptyBlocks > 0)
            {
                bool priority = (m_pendingStates&ChunkState.BuildVerticesNow)!=0;

                m_pendingStates &= ~ChunkStates.CurrStateBuildVertices;
                m_completedStates &= ~ChunkStates.CurrStateBuildVertices;
            
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
            if ((m_completedStates&ChunkState.LoadData)!=0)
            {
                // Wait until chunk is generated
                if ((m_completedStates&ChunkState.Generate)==0)
                    return false;

                // Wait for save to complete if it was requested
                if (
                    (m_pendingStates&ChunkState.PrepareSaveData)!=0 ||
                    (m_pendingStates&ChunkState.SaveData)!=0
                    )
                    return false;

                m_pendingStates &= ~ChunkState.Remove;
            }

            m_completedStates |= ChunkState.Remove;
            return true;
        }

        #endregion Remove chunk

        #endregion
    }
}
