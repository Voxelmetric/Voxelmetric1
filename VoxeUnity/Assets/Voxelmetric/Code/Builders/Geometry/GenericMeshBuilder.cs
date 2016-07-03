using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Builders.Geometry
{
    public class GenericMeshBuilder: IMeshBuilder
    {
        public void Build(Chunk chunk)
        {
            for (int y = 0; y<Env.ChunkSize; y++)
            {
                for (int z = 0; z < Env.ChunkSize; z++)
                {
                    for (int x = 0; x < Env.ChunkSize; x++)
                    {
                        Vector3Int localVector3Int = new Vector3Int(x, y, z);

                        Block block = chunk.blocks.GetBlock(localVector3Int);
                        if (block.type==BlockProvider.AirType)
                            continue;

                        block.BuildBlock(chunk, localVector3Int, chunk.pos+localVector3Int);
                    }
                }
            }
        }
    }
}
