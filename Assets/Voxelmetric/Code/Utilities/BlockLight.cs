using System.Threading;
using System.Collections.Generic;

public static class BlockLight
{

    public static int lightEffectRadius = 8;

    public static byte lightReduceBy = 2;

    public static void LightArea(World world, BlockPos pos)
    {

        if (Config.Toggle.UseMultiThreading)
        {
            Thread thread = new Thread(() => { LightAreaInner(world, pos); });
            thread.Start();
        }
        else
        {
            LightAreaInner(world, pos);
        }

    }

    static void LightAreaInner(World world, BlockPos pos)
    {
        List<BlockPos> chunksToUpdate = new List<BlockPos>();

        for (int x = pos.x - lightEffectRadius; x < pos.x + lightEffectRadius; x++)
        {
            for (int z = pos.z - lightEffectRadius; z < pos.z + lightEffectRadius; z++)
            {
                ResetLightColumn(world, x, z, chunksToUpdate);
            }
        }

        for (int x = pos.x - lightEffectRadius - 1; x < pos.x + lightEffectRadius + 1; x++)
        {
            for (int z = pos.z - lightEffectRadius - 1; z < pos.z + lightEffectRadius + 1; z++)
            {
                for (int y = Config.Env.WorldMaxY - 1; y >= Config.Env.WorldMinY; y--)
                {
                    FloodLight(world, x, y, z, chunksToUpdate);
                }
            }
        }

        world.GetChunk(pos).UpdateNow();
        world.UpdateAdjacentChunks(pos);

        foreach (var chunkPos in chunksToUpdate)
        {
            world.GetChunk(chunkPos).UpdateNow();
        }
    }

    public static void ResetLightChunkColumn(World world, Chunk chunk)
    {
        List<BlockPos> chunks = new List<BlockPos>();
        for (int x = chunk.pos.x; x < chunk.pos.x + Config.Env.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Config.Env.ChunkSize; z++)
            {
                ResetLightColumn(world, x, z, chunks);
            }
        }
        
    }

    public static void FloodLightChunkColumn(World world, Chunk chunk){
        List<BlockPos> chunks = new List<BlockPos>();
        for (int x = chunk.pos.x; x < chunk.pos.x + Config.Env.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Config.Env.ChunkSize; z++)
            {
                for (int y = chunk.pos.y; y < chunk.pos.y + Config.Env.ChunkSize; y++)
                {
                    FloodLight(world, x, y, z, chunks, chunk);
                }
            }
        }
    }

    public static void ResetLightColumn(World world, int x, int z, List<BlockPos> chunksToUpdate)
    {
        bool sunlightObstructed = false;
        for (int y = Config.Env.WorldMaxY - 1; y >= Config.Env.WorldMinY; y--)
        {
            Chunk chunk = world.GetChunk(new BlockPos(x, y, z));
            BlockPos localPos = new BlockPos(x, y, z).Subtract(chunk.pos);

            if (chunk == null){
                continue;
            }

            Block block = chunk.GetBlock(localPos);

            if (!block.controller.IsTransparent())
            {
                sunlightObstructed = true;
                continue;
            }

            if (block.controller.IsTransparent())
            {
                byte originalData1 = BlockDataMap.NonSolid.Light(block);
                if (sunlightObstructed)
                {
                    BlockDataMap.NonSolid.Light(block, block.controller.LightEmitted());
                }
                else
                {
                    BlockDataMap.NonSolid.Light(block, 15);
                }

                chunk.SetBlock(localPos, block, false);

                if (BlockDataMap.NonSolid.Light(block) != originalData1 && !chunksToUpdate.Contains(chunk.pos))
                    chunksToUpdate.Add(chunk.pos);
            }

        }
    }

    public static void FloodLight(World world, int x, int y, int z, List<BlockPos> chunksToUpdate, Chunk stayWithin = null)
    {
        Block block = world.GetBlock(new BlockPos(x, y, z));

        if (!block.controller.IsTransparent() & block.controller.LightEmitted() == 0)
            return;

        byte lightSpill = BlockDataMap.NonSolid.Light(block);
        if (block.controller.LightEmitted() > lightSpill)
            lightSpill = block.controller.LightEmitted();

        if (lightSpill <= lightReduceBy)
            return;

        lightSpill -= lightReduceBy;

        SpillLight(world, new BlockPos(x + 1, y, z), lightSpill, chunksToUpdate, stayWithin);
        SpillLight(world, new BlockPos(x, y + 1, z), lightSpill, chunksToUpdate, stayWithin);
        SpillLight(world, new BlockPos(x, y, z + 1), lightSpill, chunksToUpdate, stayWithin);
        SpillLight(world, new BlockPos(x - 1, y, z), lightSpill, chunksToUpdate, stayWithin);
        SpillLight(world, new BlockPos(x, y - 1, z), lightSpill, chunksToUpdate, stayWithin);
        SpillLight(world, new BlockPos(x, y, z - 1), lightSpill, chunksToUpdate, stayWithin);
    }

    public static void SpillLight(World world, BlockPos pos, byte light, List<BlockPos> chunksToUpdate, Chunk chunk = null)
    {
        bool stayWithinChunk = true;
        if (chunk == null)
        {
            chunk = world.GetChunk(pos);
            stayWithinChunk = false;
        }

        BlockPos localPos = pos.Subtract(chunk.pos);

        if (!Chunk.InRange(localPos))
            return;

        Block block = chunk.GetBlock(localPos);

        if (!block.controller.IsTransparent())
            return;

        if (BlockDataMap.NonSolid.Light(block) >= light)
            return;

        if (!chunksToUpdate.Contains(chunk.pos))
            chunksToUpdate.Add(chunk.pos);

        BlockDataMap.NonSolid.Light(block, light);
        chunk.SetBlock(localPos, block, false);

        byte blockLight = BlockDataMap.NonSolid.Light(block);

        if (blockLight > lightReduceBy)
        {
            BlockDataMap.NonSolid.Light(block, (byte)(blockLight - lightReduceBy));

            CallSpillLight(world, chunk, pos.Add(1, 0, 0), blockLight, chunksToUpdate, stayWithinChunk);
            CallSpillLight(world, chunk, pos.Add(0, 1, 0), blockLight, chunksToUpdate, stayWithinChunk);
            CallSpillLight(world, chunk, pos.Add(0, 0, 1), blockLight, chunksToUpdate, stayWithinChunk);
            CallSpillLight(world, chunk, pos.Add(-1, 0, 0), blockLight, chunksToUpdate, stayWithinChunk);
            CallSpillLight(world, chunk, pos.Add(0, -1, 0), blockLight, chunksToUpdate, stayWithinChunk);
            CallSpillLight(world, chunk, pos.Add(0, 0, -1), blockLight, chunksToUpdate, stayWithinChunk);
        }
        return;
    }

    static void CallSpillLight(World world, Chunk chunk, BlockPos pos, byte light, List<BlockPos> chunksToUpdate, bool stayWithinChunk)
    {
        BlockPos localPos = pos.Subtract(chunk.pos);
        if (Chunk.InRange(localPos))
        {
            SpillLight(world, pos, light, chunksToUpdate, chunk);
        }
        else
        {
            if(!stayWithinChunk)
                SpillLight(world, pos, light, chunksToUpdate);
        }
    }
}
