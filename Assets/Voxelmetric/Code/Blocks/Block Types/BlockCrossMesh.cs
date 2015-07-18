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

    public override void OnCreate(Chunk chunk, BlockPos pos, Block block)
    {
        if (block.data2 == 0)
        {
            block.data2 = 255;
            chunk.SetBlock(pos - chunk.pos, block, false);
        }
    }

}
