using UnityEngine;
using System.Collections;

public class EmptyChunk : Chunk
{
    public EmptyChunk(World world, BlockPos pos) : base(world, pos) { }

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
        return chunk.world.Void;
    }

    public override Block LocalGet(BlockPos localBlockPos)
    {
        return chunk.world.Void;
    }

    public override void Set(BlockPos blockPos, Block block, bool updateChunk = true, bool setBlockModified = true)
    { }
}
