using UnityEngine;
using System.Collections;

public static class BlockLight
{

    public static int lightEffectRadius = 4;

    public static byte lightReduceBy = 64;

    public static void LightArea(World world, BlockPos pos){
        Profiler.BeginSample("reset light");
        for (int x = pos.x-lightEffectRadius; x < pos.x+lightEffectRadius; x++)
        {
            for (int z = pos.z-lightEffectRadius; z < pos.z+lightEffectRadius; z++)
            {
                ResetLightColumn(world, x, z);
            }
        }
        Profiler.EndSample();
        Profiler.BeginSample("flood light");
        for (int x = pos.x - lightEffectRadius - 1; x < pos.x + lightEffectRadius + 1; x++)
        {
            for (int z = pos.z-lightEffectRadius-1; z < pos.z+lightEffectRadius+1; z++)
            {
                for (int y = Config.WorldMaxY - 1; y >= Config.WorldMinY; y--)
                {
                    FloodLight(world, x,y,z);
                }
            }
        }
        Profiler.EndSample();
    }

    public static void ResetLightChunkColumn(World world, Chunk chunk)
    {
        Profiler.BeginSample("reset light");
        for (int x = chunk.pos.x; x < chunk.pos.x + Config.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Config.ChunkSize; z++)
            {
                ResetLightColumn(world, x, z);
            }
        }
        Profiler.EndSample();
    }

    public static void ResetLightColumn(World world, int x, int z)
    {
        bool sunlightObstructed = false;
        for (int y = Config.WorldMaxY - 1; y >= Config.WorldMinY; y--)
        {
            Chunk chunk = world.GetChunk(x, y, z);
            BlockPos localPos = new BlockPos(x, y, z).Subtract(chunk.pos);

            if (chunk == null){
                continue;
            }

            if (chunk.blocks[localPos.x, localPos.y, localPos.z].type != 0)
            {
                sunlightObstructed = true;
                continue;
            }

            if(sunlightObstructed){
                chunk.blocks[localPos.x, localPos.y, localPos.z].data1 = 0;
            } else {
                chunk.blocks[localPos.x, localPos.y, localPos.z].data1 = 255;
            }
        }
    }

    public static void FloodLight(World world, int x, int y, int z)
    {
        
        SBlock block = world.GetBlock(x,y,z);
        if (block.type != (byte)BlockType.air)
            return;

        if (block.data1 <= lightReduceBy)
            return;

        byte lightSpill = block.data1;
        lightSpill -= lightReduceBy;

        SpillLight(world, new BlockPos(x + 1, y, z), lightSpill);
        SpillLight(world, new BlockPos(x, y + 1, z), lightSpill);
        SpillLight(world, new BlockPos(x, y, z + 1), lightSpill);
        SpillLight(world, new BlockPos(x - 1, y, z), lightSpill);
        SpillLight(world, new BlockPos(x, y - 1, z), lightSpill);
        SpillLight(world, new BlockPos(x, y, z - 1), lightSpill);
    }

    public static void SpillLight(World world, BlockPos pos, byte light){
        SBlock block = world.GetBlock(pos.x, pos.y, pos.z);
        if (block.type != (byte)BlockType.air)
            return;

        if(block.data1 >= light)
            return;

        Chunk chunk = world.GetChunk(pos.x, pos.y, pos.z);
        BlockPos localPos = pos.Subtract(chunk.pos);

        chunk.blocks[localPos.x, localPos.y, localPos.z].data1 = light;
        chunk.update = true;

        light -= lightReduceBy;
        if (light >= lightReduceBy)
        {
            SpillLight(world, localPos.Add(1, 0, 0), light);
            SpillLight(world, localPos.Add(0, 1, 0), light);
            SpillLight(world, localPos.Add(0, 0, 1), light);
            SpillLight(world, localPos.Add(-1, 0, 0), light);
            SpillLight(world, localPos.Add(0, -1, 0), light);
            SpillLight(world, localPos.Add(0, 0, -1), light);
        }
    }
}
