using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

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
    public override void RandomUpdate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        WorldBlocks blocks = chunk.world.blocks;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    BlockPos newPos = globalPos.Add(x, y, z);
                    if (blocks.Get(newPos).Equals(dirt) &&
                        blocks.Get(globalPos.Add(x, y+1, z)).Equals(air))
                    {
                        blocks.Modify(newPos, grass, true);
                    }
                }
            }
        }

        chunk.RequestBuildVertices();
    }
}