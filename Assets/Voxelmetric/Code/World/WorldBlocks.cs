using UnityEngine;
using System.Collections;

public class WorldBlocks  {

    World world;

    public WorldBlocks(World world)
    {
        this.world = world;
    }

    /// <summary> Gets the block at pos </summary>
    /// <param name="pos">Global position of the block</param>
    /// <returns>The block at the given global coordinates</returns>
    public Block Get(BlockPos pos)
    {
        Chunk containerChunk = world.chunks.Get(pos);

        if (containerChunk != null)
        {
            return containerChunk.blocks.Get(pos);
        }
        else
        {
            if (pos.y < world.config.minY)
            {
                return Block.Solid;
            }
            else
            {
                return Block.Air;
            }
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
                world.chunks.UpdateAdjacent(pos);
            }
        }
    }

}
