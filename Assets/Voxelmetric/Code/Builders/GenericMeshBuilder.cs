using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Builders
{
    public class GenericMeshBuilder: IMeshBuilder
    {
        public void Build(ChunkBlocks blocks)
        {
            for (int i = 0; i < Env.ChunkVolume; i++)
            {
                Block block = blocks.GetBlock(i);
                if (block.type == BlockProvider.AirType)
                    continue;

                int x, y, z;
                Helpers.GetChunkIndex3DFrom1D(i, out x, out y, out z);
                BlockPos localBlockPos = new BlockPos(x, y, z);

                block.BuildBlock(blocks.chunk, localBlockPos, blocks.chunk.pos + localBlockPos);
            }
        }
    }
}
