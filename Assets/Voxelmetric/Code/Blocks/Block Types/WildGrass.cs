using UnityEngine;
using System.Collections;

public class WildGrass : BlockNonSolid
{
    public WildGrass() : base() { }

    public override void AddBlockData(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {
        MeshBuilder.CrossMeshRenderer(chunk, pos, meshData, Config.Textures.WildGrass, block);
    }
    public override string Name() { return "wildgrass"; }
}
