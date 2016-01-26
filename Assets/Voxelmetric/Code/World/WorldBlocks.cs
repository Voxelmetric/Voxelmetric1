using UnityEngine;
using System.Collections;

public class WorldBlocks  {

    World world;

    public WorldBlocks(World world)
    {
        this.world = world;
    }

    public Block this[int x, int y, int z]
    {
        get { return this[new BlockPos(x, y, z)]; }
        set { this[new BlockPos(x, y, z)] = value; }
    }

    public Block this[BlockPos pos]
    {
        get { return Get(pos); }
        set { Set(pos, value, true, true); }
    }

    public Block Get(BlockPos pos)
    {
        Chunk containerChunk = world.chunks.Get(pos);

        if (containerChunk != null && pos.y >= world.config.minY)
        {
            return containerChunk.blocks.Get(pos);
        }
        else
        {
            return Block.Void;
        }
    }

    public void Set(BlockPos pos, string block, bool updateChunk = true, bool setBlockModified = true)
    {
        Set(pos, Block.New(block, world), updateChunk, setBlockModified);
    }

    /// <summary>
    /// Gets the chunk and sets the block at the given coordinates, updates the chunk and its
    /// neighbors if the update chunk flag is true or not set. Uses global coordinates, to use
    /// local coordinates use the chunk's SetBlock function.
    /// </summary>
    /// <param name="pos">Global position of the block</param>
    /// <param name="block">The block be placed</param>
    /// <param name="updateChunk">Optional parameter, set to false not update the chunk despite the change</param>
    public void Set(BlockPos pos, Block block, bool updateChunk = true, bool setBlockModified = true)
    {
        Chunk chunk = world.chunks.Get(pos);

        if (chunk != null)
        {
            chunk.blocks.Set(pos, block, updateChunk, setBlockModified);

            if (updateChunk)
            {
                UpdateAdjacentChunk(pos);
            }
        }
    }

    /// <summary> Updates any chunks neighboring a block position </summary>
    void UpdateAdjacentChunk(BlockPos pos)
    {
        //localPos is the position relative to the chunk's position
        BlockPos localPos = pos - pos.ContainingChunkCoordinates();

        //Checks to see if the block position is on the border of the chunk 
        //and if so update the chunk it's touching
        UpdateIfEqual(localPos.x, 0, pos.Add(-1, 0, 0));
        UpdateIfEqual(localPos.x, Config.Env.ChunkSize - 1, pos.Add(1, 0, 0));
        UpdateIfEqual(localPos.y, 0, pos.Add(0, -1, 0));
        UpdateIfEqual(localPos.y, Config.Env.ChunkSize - 1, pos.Add(0, 1, 0));
        UpdateIfEqual(localPos.z, 0, pos.Add(0, 0, -1));
        UpdateIfEqual(localPos.z, Config.Env.ChunkSize - 1, pos.Add(0, 0, 1));
    }

    void UpdateIfEqual(int value1, int value2, BlockPos pos)
    {
        if (value1 == value2)
        {
            Chunk chunk = world.chunks.Get(pos);
            if (chunk != null)
                chunk.UpdateNow();
        }
    }
}
