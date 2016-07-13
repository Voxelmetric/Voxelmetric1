using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public sealed class Chunk
    {
        private static int s_id = 0;

        public World world { get; private set; }
        public Vector3Int pos { get; private set; }
        public LocalPools pools { get; private set; }

        public IChunkStateManager stateManager { get; private set; }
        public ChunkBlocks blocks { get; private set; }
        public ChunkLogic logic { get; private set; }

        public RenderGeometryHandler GeometryHandler { get; private set; }
        public ColliderGeometryHandler ColliderGeometryHandler { get; private set; }

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
                    stateManager.RequestState(ChunkState.BuildCollider);
                else if (!value)
                    stateManager.ResetRequest(ChunkState.BuildCollider);
            }
        }

        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        public static Chunk CreateChunk(World world, Vector3Int pos, bool isDedicated)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();

            if (isDedicated)
                chunk.Init(world, pos, new ChunkStateManagerServer(chunk));
            else
                chunk.Init(world, pos, new ChunkStateManagerClient(chunk));

            return chunk;
        }

        /// <summary>
        /// Returns the position of the chunk containing this block
        /// </summary>
        /// <returns>The position of the chunk containing this block</returns>
        public static Vector3Int ContainingCoordinates(Vector3Int pos)
        {
            const int chunkPower = Env.ChunkPower;
            return new Vector3Int(
                (pos.x>>chunkPower)<<chunkPower,
                (pos.y>>chunkPower)<<chunkPower,
                (pos.z>>chunkPower)<<chunkPower);
        }

        public static void RemoveChunk(Chunk chunk)
        {
            chunk.Reset();
            chunk.world = null;
            Globals.MemPools.ChunkPool.Push(chunk);
        }

        public Chunk()
        {
            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            pools = Globals.WorkPool.GetPool(ThreadID);

            stateManager = new ChunkStateManagerClient(this);
            blocks = new ChunkBlocks(this);
            logic = new ChunkLogic(this);

            GeometryHandler = new RenderGeometryHandler(this);
            ColliderGeometryHandler = new ColliderGeometryHandler(this);
        }

        private void Init(World world, Vector3Int pos, IChunkStateManager stateManager)
        {
            this.world = world;
            this.pos = pos;
            this.stateManager = stateManager;

            const int size = Env.ChunkSize;
            WorldBounds = new Bounds(
                new Vector3(pos.x+size/2, pos.y+size/2, pos.z+size/2),
                new Vector3(size, size, size)
                );

            Reset();

            blocks.Init();
            stateManager.Init();
        }

        private void Reset()
        {
            stateManager.Reset();
            blocks.Reset();
            logic.Reset();

            GeometryHandler.Reset();
            ColliderGeometryHandler.Reset();

            NeedsCollider = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pos);
            sb.Append(", blocks=");
            sb.Append(blocks);
            sb.Append(", logic=");
            sb.Append(logic);
            sb.Append(", render=");
            sb.Append(GeometryHandler);
            sb.Append(", ");
            sb.Append(stateManager);
            return sb.ToString();
        }

        public bool CanUpdate
        {
            get { return stateManager.CanUpdate(); }
        }

        public void UpdateState()
        {
            // Do not update our chunk until it has all its data prepared
            if (stateManager.IsStateCompleted(ChunkState.LoadData))
            {
                logic.Update();
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
                ColliderGeometryHandler.Reset();
                return false;
            }

            // Build collider if necessary
            if (stateManager.IsStateCompleted(ChunkState.BuildCollider))
            {
                stateManager.SetColliderBuilt();
                ColliderGeometryHandler.Commit();
                return true;
            }

            return false;
        }

        public bool UpdateRenderGeometry()
        {
            // Build chunk mesh if necessary
            if (stateManager.IsStateCompleted(ChunkState.BuildVertices|ChunkState.BuildVerticesNow))
            {
                stateManager.SetMeshBuilt();
                GeometryHandler.Commit();
                return true;
            }

            return false;
        }
    }
}