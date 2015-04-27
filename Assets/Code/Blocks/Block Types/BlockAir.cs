using UnityEngine;
using System.Collections;

public class BlockAir : Block
{
    public static int health = 0;
    public static int toughness = 0;
    public static bool canBeWalkedOn = false;

    public BlockAir() : base() { }

    public override bool IsSolid(Direction direction) { return false; }

    public override void CalculateLight(Chunk chunk, BlockPos pos, SBlock sBlock)
    {
        if(pos.y == 64)
        {

        }
    }

}