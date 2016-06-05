using System.Collections.Generic;

public class WorldChunks {

    World world;

    Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();
    public ICollection<Chunk> chunkCollection { get { return chunks.Values; } }
    public ICollection<BlockPos> posCollection { get { return chunks.Keys; } }

    public WorldChunks(World world) {
        this.world = world;
    }

    public Chunk this[int x, int y, int z]
    {
        get { return this[new BlockPos(x, y, z)]; }
    }

    public Chunk this[BlockPos pos]
    {
        get { return Get(pos); }
    }

    /// <summary> Returns the chunk at the given position </summary>
    /// <param name="pos">Position of the chunk or of a block within the chunk</param>
    /// <returns>The chunk that contains the given block position or null if there is none</returns>
    public Chunk Get(BlockPos pos) {
        pos = pos.ContainingChunkCoordinates();

        Chunk containerChunk = null;
        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
    }

    public void Set(BlockPos pos, Chunk chunk) {
        pos = pos.ContainingChunkCoordinates();
        chunks[pos] = chunk;
    }

    public void Remove(BlockPos pos)
    {
        chunks.Remove(pos);
    }

    /// <summary> Instantiates a chunk and chunks at all positions surrounding it </summary>
    /// <param name="pos">The world position to create this chunk.</param>
    /// <returns>The chunk created in the center<returns>
    public Chunk New(BlockPos pos)
    {
        pos = pos.ContainingChunkCoordinates();

        //Create neighbors
        for (int x = -1; x <= 1; x++)
        {
            for (int z = -1; z <= 1; z++)
            {
                for (int y = -1; y <= 1; y++)
                {
                    if (y >= world.config.minY)
                    {
                        BlockPos newChunkPos = pos.Add(x * Config.Env.ChunkSize, y * Config.Env.ChunkSize, z * Config.Env.ChunkSize);

                        if (Get(newChunkPos) == null)
                        {
                            chunks.Add(newChunkPos, Chunk.Create(world, newChunkPos));
                        }
                    }
                }
            }
        }

        Chunk newChunk = Get(pos);
        newChunk.StartLoading();

        return newChunk;
    }
}
