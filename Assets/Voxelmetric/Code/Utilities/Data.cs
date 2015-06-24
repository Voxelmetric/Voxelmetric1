using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public static class Data
{
    public static BlockPos[] chunkLoadOrder;

    static Data()
    {
        var chunkLoads = new List<BlockPos>();
        for (int x = -Config.Env.ChunkLoadRadius; x <= Config.Env.ChunkLoadRadius; x++)
        {
            for (int z = -Config.Env.ChunkLoadRadius; z <= Config.Env.ChunkLoadRadius; z++)
            {
                chunkLoads.Add(new BlockPos(x, 0, z));
            }
        }

        // limit how far away the blocks can be to achieve a circular loading pattern
        float maxRadius = Config.Env.ChunkLoadRadius * 1.55f;

        //sort 2d vectors by closeness to center
        chunkLoadOrder = chunkLoads
                            .Where(pos => Mathf.Abs(pos.x) + Mathf.Abs(pos.z) < maxRadius)
                            .OrderBy(pos => Mathf.Abs(pos.x) + Mathf.Abs(pos.z)) //smallest magnitude vectors first
                            .ThenBy(pos => Mathf.Abs(pos.x)) //make sure not to process e.g (-10,0) before (5,5)
                            .ThenBy(pos => Mathf.Abs(pos.z))
                            .ToArray();
    }

}