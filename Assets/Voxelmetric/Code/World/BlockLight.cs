using System.Threading;
using System.Collections.Generic;

public static class BlockLight
{

    public static int lightEffectRadius = 4;

    public static byte lightReduceBy = 64;

    public static void LightArea(World world, BlockPos pos){

        Thread thread = new Thread(() =>
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

           foreach (var chunkPos in chunksToUpdate)
           {
               world.GetChunk(chunkPos).UpdateChunk();
           }

       });
        thread.Start();

    }

    public static void ResetLightChunkColumn(World world, Chunk chunk)
    {
        for (int x = chunk.pos.x -1; x < chunk.pos.x + Config.Env.ChunkSize +1; x++)
        {
            for (int z = chunk.pos.z -1; z < chunk.pos.z + Config.Env.ChunkSize +1; z++)
            {
                ResetLightColumn(world, x, z, new List<BlockPos>());
            }
        }
        
    }

    public static void FloodLightChunkColumn(World world, Chunk chunk){
        
        for (int x = chunk.pos.x; x < chunk.pos.x + Config.Env.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Config.Env.ChunkSize; z++)
            {
                for (int y = Config.Env.WorldMaxY - 1; y >= Config.Env.WorldMinY; y--)
                {
                    FloodLight(world, x, y, z, new List<BlockPos>());
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

            if (block.type != 0)
            {
                sunlightObstructed = true;
                continue;
            }

            if (sunlightObstructed)
            {
                block.data1 = 0;
            }
            else
            {
                block.data1 = 255;
            }

            chunk.SetBlock(localPos, block, false);

            if(!chunksToUpdate.Contains(chunk.pos))
                chunksToUpdate.Add(chunk.pos);

        }
    }

    public static void FloodLight(World world, int x, int y, int z, List<BlockPos> chunksToUpdate)
    {
        
        Block block = world.GetBlock(new BlockPos(x, y, z));
        if (block.type != Block.Air.type)
            return;

        if (block.data1 <= lightReduceBy)
            return;

        byte lightSpill = block.data1;
        lightSpill -= lightReduceBy;
    
        SpillLight(world, new BlockPos(x + 1, y, z), lightSpill, chunksToUpdate);
        SpillLight(world, new BlockPos(x, y + 1, z), lightSpill, chunksToUpdate);
        SpillLight(world, new BlockPos(x, y, z + 1), lightSpill, chunksToUpdate);
        SpillLight(world, new BlockPos(x - 1, y, z), lightSpill, chunksToUpdate);
        SpillLight(world, new BlockPos(x, y - 1, z), lightSpill, chunksToUpdate);
        SpillLight(world, new BlockPos(x, y, z - 1), lightSpill, chunksToUpdate);
    }

    public static void SpillLight(World world, BlockPos pos, byte light, List<BlockPos> chunksToUpdate, Chunk chunk = null){

        if(chunk==null){
            chunk = world.GetChunk(pos);
        }

        BlockPos localPos = pos.Subtract(chunk.pos);
        Block block = chunk.GetBlock(localPos);

        if (block.type != Block.Air.type)
            return;

        if (block.data1 >= light)
            return;

        if (!chunksToUpdate.Contains(chunk.pos))
            chunksToUpdate.Add(chunk.pos);

        block.data1 = light;
        chunk.SetBlock(localPos, block, false);

        if (light > lightReduceBy)
        {
            light -= lightReduceBy;

            CallSpillLight(world, chunk, pos.Add(1, 0, 0), light, chunksToUpdate);
            CallSpillLight(world, chunk, pos.Add(0, 1, 0), light, chunksToUpdate);
            CallSpillLight(world, chunk, pos.Add(0, 0, 1), light, chunksToUpdate);
            CallSpillLight(world, chunk, pos.Add(-1, 0, 0), light, chunksToUpdate);
            CallSpillLight(world, chunk, pos.Add(0, -1, 0), light, chunksToUpdate);
            CallSpillLight(world, chunk, pos.Add(0, 0, -1), light, chunksToUpdate);
        }
        return;
    }

    static void CallSpillLight(World world, Chunk chunk, BlockPos pos, byte light, List<BlockPos> chunksToUpdate)
    {
        BlockPos localPos = pos.Subtract(chunk.pos);
        if (Chunk.InRange(localPos))
        {
            SpillLight(world, pos, light, chunksToUpdate, chunk);
        }
        else
        {
            SpillLight(world, pos, light, chunksToUpdate);
        }
    }
}
