using UnityEngine;
using System.Collections;

public class BlockAir : Block
{

    public BlockAir() : base() { }

    public override bool IsSolid(Direction direction) { return false; }

}   