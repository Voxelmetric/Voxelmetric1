using UnityEngine;
using System.Collections;

public class BlockAir : BlockController
{

    public BlockAir() : base() { }

    public override bool IsSolid(Block block, Direction direction) { return false; }

    public override string Name(Block block) { return "air"; }

    public override bool IsTransparent(Block block) { return true; }
}   