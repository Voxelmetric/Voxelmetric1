using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

// This class inherits from BlockCube so that it renders just like any other
// cube block but it replaces the RandomUpdate function with its own
// Use this class for a block by setting the config's controller to GrassOverride

public class GrassBlock: CubeBlock
{
    private BlockData dirt;
    private BlockData air;
    private BlockData grass;

    public override void OnInit(BlockProvider blockProvider)
    {
        dirt = new BlockData(blockProvider.GetBlock("dirt").type);
        air = new BlockData(blockProvider.GetBlock("air").type);
        grass = new BlockData(blockProvider.GetBlock("grass").type);
    }

    // On random Update spread grass to any nearby dirt blocks on the surface
    public override void RandomUpdate(Chunk chunk, Vector3Int localPos)
    {
        ChunkBlocks blocks = chunk.blocks;

        int minX = localPos.x<=0 ? 0 : 1;
        int maxX = localPos.x>=Env.ChunkMask ? 0 : 1;
        int minY = localPos.y<=0 ? 0 : 1;
        int maxY = localPos.y>=Env.ChunkMask ? 0 : 1;
        int minZ = localPos.z<=0 ? 0 : 1;
        int maxZ = localPos.z>=Env.ChunkMask ? 0 : 1;

        for (int x = -minX; x<=maxX; x++)
        {
            for (int y = -minY; y<=maxY; y++)
            {
                for (int z = -minZ; z<=maxZ; z++)
                {
                    Vector3Int newPos = localPos.Add(x, y, z);
                    if (!blocks.Get(newPos).Equals(dirt))
                        continue;

                    // Let's turn air about dirt into grass
                    if (blocks.Get(newPos.Add(0,1,0)).Equals(air))
                        blocks.Modify(newPos, grass, true);
                }
            }
        }
    }
}