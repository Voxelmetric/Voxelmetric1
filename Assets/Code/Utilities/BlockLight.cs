using UnityEngine;
using System.Collections;

public static class BlockLight
{

    public static int lightEffectRadius = 4;

    public static byte lightReduceBy = 32;

    public static void LightArea(World world, BlockPos pos){
        ResetLight(world, pos);
        FloodLight(world, pos);
    }

    public static void ResetLight(World world, BlockPos pos)
    {
        for (int x = pos.x-lightEffectRadius; x < pos.x+lightEffectRadius; x++)
        {
            for (int z = pos.z-lightEffectRadius; z < pos.z+lightEffectRadius; z++)
            {
                bool sunlightObstructed = false;
                for (int y = Config.WorldMaxY - 1; y >= Config.WorldMinY; y--)
                {
                    if (world.GetBlock(x, y, z).type != (byte)BlockType.air){
                        sunlightObstructed = true;
                        continue;
                    }

                    Chunk chunk = world.GetChunk(x, y, z);
                    
                    if(chunk==null)
                        continue;

                    BlockPos localPos = new BlockPos(x, y, z).Subtract(chunk.pos);

                    if(sunlightObstructed){
                        chunk.blocks[localPos.x, localPos.y, localPos.z].data1 = 0;
                    } else {
                        chunk.blocks[localPos.x, localPos.y, localPos.z].data1 = 255;
                    }

                }
            }
        }
    }

    public static void FloodLight(World world, BlockPos pos)
    {
        for (int x = pos.x-lightEffectRadius; x < pos.x+lightEffectRadius; x++)
        {
            for (int z = pos.z-lightEffectRadius; z < pos.z+lightEffectRadius; z++)
            {
                for (int y = Config.WorldMaxY - 1; y >= Config.WorldMinY; y--)
                {
                    SBlock block = world.GetBlock(x,y,z);
                    if (block.type != (byte)BlockType.air)
                        continue;

                    if (block.data1 <= lightReduceBy)
                        continue;

                    byte lightSpill = block.data1;
                    lightSpill -= lightReduceBy;

                    SpillLight(world, new BlockPos(x + 1, y, z), lightSpill, firstBlock: true);
                    SpillLight(world, new BlockPos(x, y + 1, z), lightSpill, firstBlock: true);
                    SpillLight(world, new BlockPos(x, y, z + 1), lightSpill, firstBlock: true);
                    SpillLight(world, new BlockPos(x - 1, y, z), lightSpill, firstBlock: true);
                    SpillLight(world, new BlockPos(x, y - 1, z), lightSpill, firstBlock: true);
                    SpillLight(world, new BlockPos(x, y, z - 1), lightSpill, firstBlock: true);
                }
            }
        }
    }

    public static void SpillLight(World world, BlockPos pos, byte light, bool firstBlock = false){
        SBlock block = world.GetBlock(pos.x, pos.y, pos.z);

        if (block.type != (byte)BlockType.air)
            return;

        if(!firstBlock && block.data1 >= light)
            return;
        
        Chunk chunk = world.GetChunk(pos.x, pos.y, pos.z);
        BlockPos localPos = pos.Subtract(chunk.pos);

        if(block.data1 < light)
            chunk.blocks[localPos.x, localPos.y, localPos.z].data1 = light;

        byte lightSpill = block.data1;
        lightSpill -= lightReduceBy;

        SpillLight(world, localPos.Add(1, 0, 0), lightSpill);
        SpillLight(world, localPos.Add(0, 1, 0), lightSpill);
        SpillLight(world, localPos.Add(0, 0, 1), lightSpill);
        SpillLight(world, localPos.Add(-1, 0, 0), lightSpill);
        SpillLight(world, localPos.Add(0, -1, 0), lightSpill);
        SpillLight(world, localPos.Add(0, 0, -1), lightSpill);
    }
}
