using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core.GeometryHandler;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public sealed class Chunk
    {
        //! ID used by memory pools to map the chunk to a given thread. Must be accessed from the main thread
        private static int s_id = 0;

        public World world { get; private set; }
        public IChunkStateManager stateManager { get; private set; }
        public ChunkBlocks blocks { get; private set; }
        public ChunkLogic logic { get; private set; }
        public ChunkRenderGeometryHandler GeometryHandler { get; private set; }
        public ChunkColliderGeometryHandler ChunkColliderGeometryHandler { get; private set; }
        public LocalPools pools { get; private set; }

        //! Chunk position in world coordinates
        public Vector3Int pos { get; private set; }

        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

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
                    stateManager.RequestState(ChunkState.BuildCollider);
                else if (!value)
                    stateManager.ResetRequest(ChunkState.BuildCollider);
            }
        }


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
            return new Vector3Int(
                (pos.x>>Env.ChunkPow)<<Env.ChunkPow,
                (pos.y>>Env.ChunkPow)<<Env.ChunkPow,
                (pos.z>>Env.ChunkPow)<<Env.ChunkPow);
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


            GeometryHandler = new ChunkRenderGeometryHandler(this);
            ChunkColliderGeometryHandler = new ChunkColliderGeometryHandler(this);
        }

        private void Init(World world, Vector3Int pos, IChunkStateManager stateManager)
        {
            this.world = world;
            this.pos = pos;
            this.stateManager = stateManager;

            logic = world.config.randomUpdateFrequency>0.0f ? new ChunkLogic(this) : null;

            WorldBounds = new Bounds(
                new Vector3(pos.x+ Env.ChunkSize/2, pos.y+ Env.ChunkSize/2, pos.z+ Env.ChunkSize/2),
                new Vector3(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize)
                );

            Reset();

            blocks.Init();
            stateManager.Init();
        }

        private void Reset()
        {
            stateManager.Reset();
            blocks.Reset();
            if (logic!=null)
                logic.Reset();

            GeometryHandler.Reset();
            ChunkColliderGeometryHandler.Reset();

            NeedsCollider = false;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pos);
            sb.Append(", blocks=");
            sb.Append(blocks);
            if (logic!=null)
            {
                sb.Append(", logic=");
                sb.Append(logic);
            }
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
                if (logic!=null)
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
                ChunkColliderGeometryHandler.Reset();
                return false;
            }

            // Build collider if necessary
            if (stateManager.IsStateCompleted(ChunkState.BuildCollider))
            {
                stateManager.SetColliderBuilt();
                ChunkColliderGeometryHandler.Commit();
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