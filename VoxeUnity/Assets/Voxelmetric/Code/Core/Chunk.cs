using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.GeometryHandler;

namespace Voxelmetric.Code.Core
{
    public sealed class Chunk
    {
        //! ID used by memory pools to map the chunk to a given thread. Must be accessed from the main thread
        private static int s_id = 0;
        
        public World world { get; private set; }
        public ChunkStateManagerClient stateManager { get; private set; }
        public ChunkBlocks blocks { get; private set; }
        public ChunkLogic logic { get; private set; }
        public ChunkRenderGeometryHandler GeometryHandler { get; private set; }
        public ChunkColliderGeometryHandler ChunkColliderGeometryHandler { get; private set; }
        public LocalPools pools { get; private set; }

        //! Chunk position in world coordinates
        public Vector3Int pos { get; private set; }

        //! Bounding box in world coordinates. It always considers a full-size chunk
        public AABB WorldBounds;

        //! List of chunk listeners
        public Chunk[] Neighbors { get; }
        //! Number of registered listeners        
        public int NeighborCount { get; private set; }
        public int NeighborCountMax { get; private set; }

        //! Size of chunk's side
        public int SideSize { get; } = 0;

        //! Bounding coordinates in local space. Corresponds to real geometry
        public int minBounds, maxBounds;
        //! Bounding coordinates in local space. Corresponds to collision geometry
        public int minBoundsC, maxBoundsC;

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        public int MaxPendingStructureListIndex;
        public bool NeedApplyStructure;

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
                    blocks.RequestCollider();
            }
        }

        public static Chunk CreateChunk(World world, Vector3Int pos)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();
            chunk.Init(world, pos);
            return chunk;
        }        

        public static void RemoveChunk(Chunk chunk)
        {
            // Reset the chunk back to defaults
            chunk.Reset();
            chunk.world = null;

            // Return the chunk pack to object pool
            Globals.MemPools.ChunkPool.Push(chunk);
        }

        /// <summary>
        /// Returns the position of the chunk containing this block
        /// </summary>
        /// <returns>The position of the chunk containing this block</returns>
        public static Vector3Int ContainingChunkPos(ref Vector3Int pos)
        {
            return new Vector3Int(
                Helpers.MakeChunkCoordinate(pos.x),
                Helpers.MakeChunkCoordinate(pos.y),
                Helpers.MakeChunkCoordinate(pos.z)
                );
        }

        public Chunk(int sideSize = Env.ChunkSize)
        {
            SideSize = sideSize;

            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            pools = Globals.WorkPool.GetPool(ThreadID);
            
            stateManager = new ChunkStateManagerClient(this);
            blocks = new ChunkBlocks(this, sideSize);

            Neighbors = Helpers.CreateArray1D<Chunk>(6);
        }

        public void Init(World world, Vector3Int pos)
        {
            this.world = world;
            this.pos = pos;
            
            stateManager = new ChunkStateManagerClient(this);

            if (world!=null)
            {
                logic = world.config.randomUpdateFrequency>0.0f ? new ChunkLogic(this) : null;

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
            minBounds = maxBounds = 0;
            minBoundsC = maxBoundsC = 0;

            Reset();

            blocks.Init();
            stateManager.Init();

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

            stateManager.Reset();
            blocks.Reset();
            if (logic!=null)
                logic.Reset();

            GeometryHandler.Reset();
            ChunkColliderGeometryHandler.Reset();

            m_needsCollider = false;
            NeedApplyStructure = true;
            MaxPendingStructureListIndex = 0;

            //chunk.world = null; <-- must not be done inside here! Do it outside the method
        }
        
        public bool CanUpdate
        {
            get { return stateManager.CanUpdate(); }
        }

        public void UpdateState()
        {
            // Do not update our chunk until it has all its data prepared
            if (stateManager.IsStateCompleted(ChunkState.Generate))
            {
                // Apply pending structures
                world.ApplyPendingStructures(this);

                // Update logic
                if (logic!=null)
                    logic.Update();

                // Update blocks
                blocks.Update();
            }

            // Process chunk tasks
            stateManager.Update();
        }

        public bool UpdateCollisionGeometry()
        {
            // Release the collider when no longer needed
            if (!NeedsCollider)
            {
                stateManager.SetColliderBuilt();
                ChunkColliderGeometryHandler.Reset();
                return false;
            }

            // Build collision geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget)
                return false;
            
            // Build collider if necessary
            if (stateManager.IsStateCompleted(ChunkStates.CurrStateBuildCollider))
            {
                Profiler.BeginSample("UpdateCollisionGeometry");
                Globals.GeometryBudget.StartMeasurement();

                stateManager.SetColliderBuilt();
                ChunkColliderGeometryHandler.Commit();

                Globals.GeometryBudget.StopMeasurement();
                Profiler.EndSample();
                return true;
            }
            
            return false;
        }

        public bool UpdateRenderGeometry()
        {
            // Build render geometry only if there is enough time
            if (!Globals.GeometryBudget.HasTimeBudget)
                return false;
            
            // Build chunk mesh if necessary
            if (stateManager.IsStateCompleted(ChunkStates.CurrStateBuildVertices))
            {
                Profiler.BeginSample("UpdateRenderGeometry");
                Globals.GeometryBudget.StartMeasurement();

                stateManager.SetMeshBuilt();
                GeometryHandler.Commit();

                Globals.GeometryBudget.StopMeasurement();
                Profiler.EndSample();
                return true;
            }
            
            return false;
        }

        #region Neighbors

        public bool RegisterNeighbor(Chunk neighbor)
        {
            if (neighbor == null || neighbor == this)
                return false;

            // Determine neighbors's direction as compared to current chunk
            Vector3Int p = pos - neighbor.pos;
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
            Vector3Int p = pos - neighbor.pos;
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
            ChunkStateManagerClient stateManager = chunk.stateManager;
            World world = chunk.world;
            if (world == null)
                return;

            // Calculate how many listeners a chunk can have
            int maxListeners = 0;
            Vector3Int pos = chunk.pos;
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

            //int prevListeners = stateManager.ListenerCountMax;

            // Update max listeners and request geometry update
            chunk.NeighborCountMax = maxListeners;

            // Request synchronization of edges and build geometry
            //if(prevListeners<maxListeners)
            stateManager.m_syncEdgeBlocks = true;

            // Geometry needs to be rebuild
            stateManager.SetStatePending(ChunkState.BuildVertices);

            // Collider might beed to be rebuild
            if (chunk.NeedsCollider)
                chunk.blocks.RequestCollider();
        }

        private void SubscribeNeighbors(bool subscribe)
        {
            SubscribeTwoNeighbors(pos.Add(Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(-Env.ChunkSize, 0, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, -Env.ChunkSize, 0), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, 0, Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(pos.Add(0, 0, -Env.ChunkSize), subscribe);

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
    }
}