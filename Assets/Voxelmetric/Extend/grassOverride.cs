using UnityEngine;
using System.Collections;


// This class inherits from BlockCube so that it renders just like any other
// cube block but it replaces the RandomUpdate function with its own
// Use this class for a block by setting the config's controller to GrassOverride
public class GrassOverride : BlockCube
{
    //On random update spread grass to any nearby dirt blocks on the surface
    public override void RandomUpdate(Chunk chunk, BlockPos pos, Block block)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (chunk.GetBlock(pos.Add(x, y, z)) == new Block("dirt", chunk.world)
                        && chunk.GetBlock(pos.Add(x, y + 1, z)) == new Block("air", chunk.world))
                    {
                        chunk.SetBlock(pos.Add(x, y, z), "grass", false);
                        chunk.SetFlag(Chunk.Flag.updateSoon, true);
                    }
                }
            }
        }
    }
}
