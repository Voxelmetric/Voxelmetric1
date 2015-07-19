using UnityEngine;
using System.Collections;

public class grassOverride : BlockOverride
{
    Block dirt = "dirt";
    Block air = "air";

    //On random update spread grass to any nearby dirt blocks on the surface
    public override void RandomUpdate(Chunk chunk, BlockPos pos, Block block)
    {
        for (int x = -1; x <= 1; x++)
        {
            for (int y = -1; y <= 1; y++)
            {
                for (int z = -1; z <= 1; z++)
                {
                    if (chunk.GetBlock(pos.Add(x, y, z) - chunk.pos) == dirt
                        && chunk.GetBlock(pos.Add(x, y + 1, z) - chunk.pos) == air)
                    {
                        chunk.SetBlock(pos.Add(x, y, z) - chunk.pos, "grass", false);
                        chunk.SetFlag(Chunk.Flag.updateSoon, true);
                    }
                }
            }
        }
    }
}
