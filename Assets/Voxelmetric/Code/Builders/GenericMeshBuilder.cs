using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Builders
{
    public class GenericMeshBuilder: IMeshBuilder
    {
        public void Build(Chunk chunk)
        {
            for (int i = 0; i < Env.ChunkVolume; i++)
            {
                Block block = chunk.blocks.GetBlock(i);
                if (block.type == BlockProvider.AirType)
                    continue;

                int x, y, z;
                Helpers.GetChunkIndex3DFrom1D(i, out x, out y, out z);
                BlockPos localBlockPos = new BlockPos(x, y, z);

                block.BuildBlock(chunk, localBlockPos, chunk.pos + localBlockPos);
            }
        }
    }
}
