public class EmptyChunk : Chunk
{
    public new static EmptyChunk Create(World world, BlockPos pos)
    {
        EmptyChunk chunk = new EmptyChunk();
        chunk.Init(world, pos);
        return chunk;
    }

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
        return chunk.world.Air;
    }

    public override Block LocalGet(BlockPos localBlockPos)
    {
        return chunk.world.Air;
    }

    public override void Set(BlockPos blockPos, Block block, bool updateChunk = true, bool setBlockModified = true)
    { }
}
