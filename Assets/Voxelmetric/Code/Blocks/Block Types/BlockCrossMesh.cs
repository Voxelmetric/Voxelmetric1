using UnityEngine;
using System.Collections;

public class BlockCrossMesh : BlockNonSolid
{
    public BlockCrossMesh() : base() { }

    public TextureCollection texture;
    public string blockName;

    public override void AddBlockData(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {
        MeshBuilder.CrossMeshRenderer(chunk, pos, meshData, texture, block);
    }
    public override string Name() { return blockName; }

    public override bool IsTransparent() { return true; }

}
