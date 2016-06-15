using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Assets.Voxelmetric.Code.Builders
{
    public class GenericMeshBuilder: IMeshBuilder
    {
        public void Build(ChunkBlocks blocks)
        {
            for (int i = 0; i < Env.ChunkVolume; i++)
            {
                int x, y, z;
                Helpers.GetChunkIndex3DFrom1D(i, out x, out y, out z);
                BlockPos localBlockPos = new BlockPos(x, y, z);

                Block block = blocks.GetBlock(i);
                if (block.type == BlockProvider.AirType)
                    continue;

                block.BuildBlock(blocks.chunk, localBlockPos, localBlockPos + blocks.chunk.pos);
            }
        }
    }
}
