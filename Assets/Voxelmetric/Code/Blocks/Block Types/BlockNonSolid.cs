using UnityEngine;
using System.Collections;

public class BlockNonSolid : BlockController
{
    public BlockNonSolid() : base() { }

    public override void AddBlockData(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {

    }

    public override string Name() { return "nonsolid"; }

    public override bool IsSolid(Direction direction) { return false; }

    public override bool IsTransparent() { return true; }

}
