using Voxelmetric.Code.Core.Interface;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public class ChunkRender: IChunkRender
    {
        protected DrawCallBatcher m_drawCallBatcher;
        protected Chunk chunk;

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
            foreach (BlockPos localBlockPos in new BlockPosEnumerable(Env.ChunkSizePos))
            {
                Block block = chunk.blocks.LocalGet(localBlockPos);
                if (block.type==0)
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