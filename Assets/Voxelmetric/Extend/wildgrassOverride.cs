using UnityEngine;
using System.Collections;

public class wildgrassOverride : BlockOverride
{

    // On create set the height to 10 and schedule and update in 1 second
    public override Block OnCreate(Chunk chunk, BlockPos pos, Block block)
    {
        //Debug.Log("Height: " + BlockDataMap.CrossMesh.Height(block,  chunk.world.noiseGen.Generate(pos.x * 1000, pos.y * 1000, pos.z * 1000) ));
        //Debug.Log("XOffset: " + BlockDataMap.CrossMesh.XOffset(block, (chunk.world.noiseGen.Generate(pos.x * 1000, pos.y * 1000, pos.z * 1000) + 0.5f)));
        //Debug.Log("YOffset: " + BlockDataMap.CrossMesh.YOffset(block, (chunk.world.noiseGen.Generate(pos.x * 1000, pos.y * 10000, pos.z * 1000) + 0.5f)));

        return block;
    }
}
