using UnityEngine;
using System.Collections;

public class wildgrassOverride : BlockOverride
{

    // On create set the height to 10 and schedule and update in 1 second
    public override Block OnCreate(Chunk chunk, BlockPos pos, Block block)
    {
        block.data2 = (byte)(64 + ((chunk.world.noiseGen.Generate(pos.x * 1000, pos.y * 1000, pos.z * 1000) + 1) * 96));
        int offset1 = (int)((chunk.world.noiseGen.Generate(pos.x* 1000, pos.y* 1000, pos.z * 1000) + 1) * 16);
        block.data3 = (byte)((block.data3 & 240) | (offset1 & 15));
        int offset2 = (int)((chunk.world.noiseGen.Generate(pos.x*1000, pos.y * 10000, pos.z * 1000) + 1) * 16);
        block.data3 = (byte)((offset2 << 4) | (block.data3 & 15));

        return block;
    }

    //On random update add 100 to the height
    //Removed this because although it is nice it means every block with wild grass needs to get saved
    //public override void RandomUpdate(Chunk chunk, BlockPos pos, Block block)
    //{
    //    block.data2 += 100;
    //    chunk.SetBlock(pos, block);
    //}
}
