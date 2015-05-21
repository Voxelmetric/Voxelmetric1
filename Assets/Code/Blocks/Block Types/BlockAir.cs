using UnityEngine;
using System.Collections;

public class BlockAir : BlockController
{

    public BlockAir() : base() { }

    public override bool IsSolid(Direction direction) { return false; }

}   