using UnityEngine;
using System.Collections;

public class EmptyChunk : Chunk
{
    public override void StartLoading()
    {
        blocks = new EmptyChunkBlocks(this);
    }
}

public class EmptyChunkBlocks : ChunkBlocks
{
    public EmptyChunkBlocks(Chunk chunk) : base(chunk) { }

    public override Block Get(BlockPos blockPos)
    {
        return Block.Air;
    }

    public override Block LocalGet(BlockPos localBlockPos)
    {
        return Block.Air;
    }

    public override void Set(BlockPos blockPos, Block block, bool updateChunk = true, bool setBlockModified = true)
    { }
}
