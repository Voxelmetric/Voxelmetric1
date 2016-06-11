using System;

[Serializable]
public class TexturedCube : CubeBlock
{
    //public string[] textureList = new string[] { "actionmine"};
    //public TextureCollection[] textureCollections;

    //public override void SetUpController(BlockConfig config, World world)
    //{
    //    blockName = config.name;
    //    textureCollections = new TextureCollection[textureList.Length];
    //    for (int i = 0; i < textureList.Length; i++)
    //    {
    //        textureCollections[i] = world.textureIndex.GetTextureCollection(textureList[i]);
    //    }
    //    isSolid = config.isSolid;
    //    solidTowardsSameType = config.solidTowardsSameType;
    //    base.SetUpController(config, world);
    //}

    //public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, VertexData vertexData, Direction direction, Block block)
    //{
    //    BlockBuilder.BuildRenderer(chunk, localPos, globalPos, vertexData, direction);
    //    BlockBuilder.BuildTexture(chunk, localPos, globalPos, vertexData, direction, new TextureCollection[] {
    //        textureCollections[block.data], textureCollections[block.data], textureCollections[block.data],
    //        textureCollections[block.data], textureCollections[block.data], textureCollections[block.data]
    //    });
    //    BlockBuilder.BuildColors(chunk, localPos, globalPos, vertexData, direction);
    //    if (block.world.config.useCollisionMesh)
    //    {
    //        BlockBuilder.BuildCollider(chunk, localPos, globalPos, vertexData, direction);
    //    }
    //}
}
