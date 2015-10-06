using UnityEngine;
using System.Collections;

public class BlockCrossMesh : BlockNonSolid
{
    public BlockCrossMesh() : base() { }

    public TextureCollection texture;
    public string blockName;

    public override void SetUpController(BlockConfig config, World world)
    {
        blockName = config.name;
        texture = world.textureIndex.GetTextureCollection(config.textures[0]);
    }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    {
        MeshBuilder.CrossMeshRenderer(chunk, localPos, globalPos, meshData, texture, block);
    }
    public override string Name() { return blockName; }

    public override bool IsTransparent() { return true; }

}
