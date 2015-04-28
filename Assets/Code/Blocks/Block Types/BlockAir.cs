using UnityEngine;
using System.Collections;

public class BlockAir : Block
{
    public static int health = 0;
    public static int toughness = 0;
    public static bool canBeWalkedOn = false;

    public BlockAir() : base() { }

    public override bool IsSolid(Direction direction) { return false; }

    //public override void CalculateLight(Chunk chunk, BlockPos pos)
    //{
    //    chunk.blocks[pos.x, pos.y, pos.z].data1 = 0;
    //    byte thisLight = chunk.blocks[pos.x, pos.y, pos.z].data1;

    //    if (pos.y + chunk.pos.y >= 64 || (chunk.GetBlock(pos.Add(0, 1, 0)).type == 0 && chunk.GetBlock(pos.Add(0, 1, 0)).data1 == 255))
    //    {
    //        chunk.blocks[pos.x, pos.y, pos.z].data1 = 255;
    //    } else {

    //        thisLight = SetLightIfOver(chunk, pos, pos.Add(1, 0, 0), thisLight);
    //        thisLight = SetLightIfOver(chunk, pos, pos.Add(0, 1, 0), thisLight);
    //        thisLight = SetLightIfOver(chunk, pos, pos.Add(0, 0, 1), thisLight);
    //        thisLight = SetLightIfOver(chunk, pos, pos.Add(-1, 0, 0), thisLight);
    //        thisLight = SetLightIfOver(chunk, pos, pos.Add(0, -1, 0), thisLight);
    //        thisLight = SetLightIfOver(chunk, pos, pos.Add(0, 0, -1), thisLight);

    //        thisLight = SetLightIfOver(chunk, pos.Add(1, 0, 0), pos, chunk.GetBlock(pos.Add(1, 0, 0)).data1);
    //        thisLight = SetLightIfOver(chunk, pos.Add(0, 1, 0), pos, chunk.GetBlock(pos.Add(0, 1, 0)).data1);
    //        thisLight = SetLightIfOver(chunk, pos.Add(0, 0, 1), pos, chunk.GetBlock(pos.Add(0, 0, 1)).data1);
    //        thisLight = SetLightIfOver(chunk, pos.Add(-1, 0, 0), pos, chunk.GetBlock(pos.Add(-1, 0, 0)).data1);
    //        thisLight = SetLightIfOver(chunk, pos.Add(0, -1, 0), pos, chunk.GetBlock(pos.Add(0, -1, 0)).data1);
    //        thisLight = SetLightIfOver(chunk, pos.Add(0, 0, -1), pos, chunk.GetBlock(pos.Add(0, 0, -1)).data1);
    //    }
    //}

    void SetLightIfOver(World world, BlockPos pos, byte currentLight)
    {
        Chunk chunk = world.GetChunk(pos.x, pos.y, pos.z);

        int x = pos.x - chunk.pos.x;
        int y = pos.y - chunk.pos.y;
        int z = pos.z - chunk.pos.z;

        if(chunk.blocks[x,y,z].type!= (byte)BlockType.air)
            return;

        byte lightSpill = currentLight;
             lightSpill -= 32;

        byte blockLight = chunk.blocks[x,y,z].data1;

        if (lightSpill > blockLight)
        {
            chunk.blocks[x, y, z].data1 = lightSpill;
        }
    }

}   