using UnityEngine;
using System.Collections;

public class BlockNonSolid : BlockController
{
    public BlockNonSolid() : base() { }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    {

    }

    public override string Name(Block block) { return "nonsolid"; }

    public override bool IsSolid(Block block, Direction direction) { return false; }

    public override bool IsTransparent(Block block) { return true; }

}
