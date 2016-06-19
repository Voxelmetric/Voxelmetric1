using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public sealed class Chunk
    {
        private static int s_id = 0;

        public World world { get; private set; }
        public BlockPos pos { get; private set; }
        public LocalPools pools { get; private set; }

        public ChunkBlocks blocks { get; private set; }
        public ChunkLogic logic { get; private set; }
        public ChunkRender render { get; private set; }
        public IChunkStateManager stateManager { get; private set; }

        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        public static Chunk CreateChunk(World world, BlockPos pos)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();
            chunk.Init(world, pos);
            return chunk;
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

            render = new ChunkRender(this);
            blocks = new ChunkBlocks(this);
            logic = new ChunkLogic(this);
            stateManager = new ChunkStateManagerClient(this);
        }

        private void Init(World world, BlockPos pos)
        {
            this.world = world;
            this.pos = pos;

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
            blocks.Reset();
            logic.Reset();
            render.Reset();
            stateManager.Reset();
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
            sb.Append(render);
            sb.Append(", ");
            sb.Append(stateManager);
            return sb.ToString();
        }
        
        public void UpdateChunk()
        {
            if (!stateManager.CanUpdate())
                return;

            // Do not update our chunk until it has all its data prepared
            if (stateManager.IsStateCompleted(ChunkState.LoadData))
            {
                logic.Update();
                blocks.Update();
            }

            // Build chunk mesh if necessary
            if (stateManager.IsStateCompleted(ChunkState.BuildVertices|ChunkState.BuildVerticesNow))
            {
                stateManager.SetMeshBuilt();
                render.BuildMesh();
            }

            // Process chunk tasks
            stateManager.Update();
        }
    }
}