using System;


// This class inherits from BlockCube so that it renders just like any other
// cube block but it replaces the RandomUpdate function with its own
// Use this class for a block by setting the config's controller to GrassOverride
[Serializable]
public class GrassBlock : CubeBlock
{
    //On random update spread grass to any nearby dirt blocks on the surface
    public override void RandomUpdate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        ChunkBlocks blocks = chunk.blocks;
        BlockIndex blockIndex = chunk.world.blockIndex;
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (
                        blocks.Get(globalPos.Add(x, y, z)).type==blockIndex.GetType("dirt") &&
                        blocks.Get(globalPos.Add(x, y+1, z)).type==blockIndex.GetType("air")
                        )
                    {
                        blocks.Set(globalPos.Add(x, y, z), "grass", false);
                    }
                }
            }
        }

        chunk.UpdateSoon();
    }
}
