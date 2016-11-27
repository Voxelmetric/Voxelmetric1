using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

namespace Voxelmetric.Code.Builders.Geometry
{
    public class GenericMeshBuilder: IMeshBuilder
    {
        public void Build(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            for (int y = minY; y<=maxY; y++)
            {
                for (int z = minZ; z<=maxZ; z++)
                {
                    for (int x = minX; x<=maxX; x++)
                    {
                        Vector3Int localVector3Int = new Vector3Int(x, y, z);

                        Block block = chunk.blocks.GetBlock(localVector3Int);
                        if (block.type==BlockProvider.AirType)
                            continue;

                        block.BuildBlock(chunk, localVector3Int);
                    }
                }
            }
        }
    }
}
