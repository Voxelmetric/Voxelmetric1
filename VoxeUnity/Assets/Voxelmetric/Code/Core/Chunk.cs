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

        public bool NeedApplyStructure;
        public int MaxPendingStructureListIndex;

        //! Chunk position in world coordinates
        public Vector3Int pos { get; private set; }

        //! Bounding box in world coordinates
        public AABB WorldBounds { get; private set; }

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

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

        private int m_sideSize = 0;
        public int SideSize
        {
            get { return m_sideSize; }
        }

        public static Chunk CreateChunk(World world, Vector3Int pos)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();
            chunk.Init(world, pos);
            return chunk;
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

        public static void RemoveChunk(Chunk chunk)
        {
            // Reset the chunk back to defaults
            chunk.Reset();
            chunk.world = null; // Can't do inside Reset!!

            // Return the chunk pack to object pool
            Globals.MemPools.ChunkPool.Push(chunk);
        }

        public Chunk(int sideSize = Env.ChunkSize)
        {
            m_sideSize = sideSize;

            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            pools = Globals.WorkPool.GetPool(ThreadID);
            
            stateManager = new ChunkStateManagerClient(this);
            blocks = new ChunkBlocks(this, sideSize);
        }

        public void Init(World world, Vector3Int pos)
        {
            this.world = world;
            this.pos = pos;
            
            stateManager = new ChunkStateManagerClient(this);
            logic = world.config.randomUpdateFrequency>0.0f ? new ChunkLogic(this) : null;

            if (GeometryHandler == null)
                GeometryHandler = new ChunkRenderGeometryHandler(this, world.renderMaterials);
            if (ChunkColliderGeometryHandler == null)
                ChunkColliderGeometryHandler = new ChunkColliderGeometryHandler(this, world.physicsMaterials);

            WorldBounds = new AABB(
                pos.x, pos.y, pos.z,
                pos.x+ m_sideSize, pos.y+ m_sideSize, pos.z+ m_sideSize
                );

            Reset();

            blocks.Init();
            stateManager.Init();
        }

        private void Reset()
        {
            NeedApplyStructure = true;
            MaxPendingStructureListIndex = 0;

            stateManager.Reset();
            blocks.Reset();
            if (logic!=null)
                logic.Reset();

            GeometryHandler.Reset();
            ChunkColliderGeometryHandler.Reset();

            m_needsCollider = false;
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
            if (stateManager.IsStateCompleted(ChunkState.BuildCollider))
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
            if (stateManager.IsStateCompleted(ChunkState.BuildVertices|ChunkState.BuildVerticesNow))
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
    }
}