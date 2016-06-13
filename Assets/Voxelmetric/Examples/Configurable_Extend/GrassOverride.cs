using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

// This class inherits from BlockCube so that it renders just like any other
// cube block but it replaces the RandomUpdate function with its own
// Use this class for a block by setting the config's controller to GrassOverride

public class GrassBlock: CubeBlock
{
    private BlockData dirt;
    private BlockData air;
    private BlockData grass;

    public override void OnInit()
    {
        dirt = new BlockData(config.world.blockProvider.GetBlock("dirt").type);
        air = new BlockData(config.world.blockProvider.GetBlock("air").type);
        grass = new BlockData(config.world.blockProvider.GetBlock("grass").type);
    }

    // On random Update spread grass to any nearby dirt blocks on the surface
    public override void RandomUpdate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        ChunkBlocks blocks = chunk.blocks;

        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    BlockPos newPos = globalPos.Add(x, y, z);
                    if (blocks.GetBlockData(newPos).Equals(dirt) &&
                        blocks.GetBlockData(globalPos.Add(x, y+1, z)).Equals(air))
                    {
                        blocks.SetBlockData(newPos, grass, false);
                    }
                }
            }
        }

        chunk.RequestBuildVertices();
    }
}