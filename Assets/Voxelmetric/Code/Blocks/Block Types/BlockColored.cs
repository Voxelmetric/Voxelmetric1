using UnityEngine;
using System.Collections;

public class BlockColored : BlockSolid {

    //public string blockName;
    //public TextureCollection textureCollection;

    //public override void SetUpController(BlockConfig config, World world)
    //{
    //    blockName = config.name;
    //    isSolid = config.isSolid;
    //    textureCollection = world.textureIndex.GetTextureCollection(config.textures[0]);
    //    solidTowardsSameType = config.solidTowardsSameType;
    //    base.SetUpController(config, world);
    //}

    //public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, Block block)
    //{
    //    BlockBuilder.BuildRenderer(chunk, localPos, globalPos, meshData, direction);

    //    Rect texture = textureCollection.GetTexture(chunk, localPos, globalPos, direction);
    //    Vector2[] UVs = new Vector2[4];

    //    UVs[0] = new Vector2(texture.x + texture.width, texture.y);
    //    UVs[1] = new Vector2(texture.x + texture.width, texture.y + texture.height);
    //    UVs[2] = new Vector2(texture.x, texture.y + texture.height);
    //    UVs[3] = new Vector2(texture.x, texture.y);

    //    meshData.uv.AddRange(UVs);

    //    meshData.colors.Add(new Color(block.data[0] / 255f, block.data[1] / 255f, block.data[2] / 255f));
    //    meshData.colors.Add(new Color(block.data[0] / 255f, block.data[1] / 255f, block.data[2] / 255f));
    //    meshData.colors.Add(new Color(block.data[0] / 255f, block.data[1] / 255f, block.data[2] / 255f));
    //    meshData.colors.Add(new Color(block.data[0] / 255f, block.data[1] / 255f, block.data[2] / 255f));

    //    if (block.world.config.useCollisionMesh)
    //    {
    //        BlockBuilder.BuildCollider(chunk, localPos, globalPos, meshData, direction);
    //    }
    //}

    //public override string Name(Block block)
    //{
    //    return blockName;
    //}

    //public override bool IsSolid(Block block, Direction direction)
    //{
    //    return isSolid;
    //}

    ///// <summary>
    ///// Sets the color data of a block. Used like this: block = SetBlockColor(255, 0, 128);
    ///// </summary>
    ///// <param name="r">red</param>
    ///// <param name="g">green</param>
    ///// <param name="b">blue</param>
    ///// <returns>A new version of the block with the color data added</returns>
    //public static Block SetBlockColor(Block block, byte r, byte g, byte b)
    //{
    //    block.data[0] = r;
    //    block.data[1] = g;
    //    block.data[2] = b;
    //    return block;
    //}
}
