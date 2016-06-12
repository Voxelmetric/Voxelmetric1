using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public class ChunkRender
    {
        private Chunk chunk;
        private DrawCallBatcher m_drawCallBatcher;

        public DrawCallBatcher batcher
        {
            get { return m_drawCallBatcher; }
        }

        public ChunkRender(Chunk chunk)
        {
            this.chunk = chunk;
            m_drawCallBatcher = new DrawCallBatcher(Globals.CubeMeshBuilder, this.chunk);
        }

        public void Reset()
        {
            m_drawCallBatcher.Clear();
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public void BuildMeshData()
        {
            for (int i = 0; i<Env.ChunkVolume; i++)
            {
                int x, y, z;
                Helpers.GetChunkIndex3DFrom1D(i, out x, out y, out z);
                BlockPos localBlockPos = new BlockPos(x, y, z);

                Block block = chunk.blocks.LocalGet(localBlockPos);
                if (block.type==BlockIndex.VoidType)
                    continue;

                block.BuildBlock(chunk, localBlockPos, localBlockPos+chunk.pos);
            }
        }

        public void BuildMesh()
        {
            m_drawCallBatcher.Commit();
        }
    }
}