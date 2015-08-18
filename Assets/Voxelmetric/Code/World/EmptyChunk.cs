using UnityEngine;
using System.Collections;

public class EmptyChunk : Chunk
{

    public override Block GetBlock(BlockPos blockPos)
    {
        return Block.Air;
    }

    public override void SetBlock(BlockPos blockPos, Block block, bool updateChunk = true) { }

    protected override void TimedUpdated() {}
}
