using UnityEngine;
using System.Collections;

public class ColoredBlock : SolidBlock {

    public Color color;
    public TextureCollection texture { get { return ((ColoredBlockConfig)config).texture; } }

    public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, localPos, globalPos, meshData, direction);

        Rect textureRect = texture.GetTexture(chunk, localPos, globalPos, direction);
        Vector2[] UVs = new Vector2[4];
        UVs[0] = new Vector2(textureRect.x + textureRect.width, textureRect.y);
        UVs[1] = new Vector2(textureRect.x + textureRect.width, textureRect.y + textureRect.height);
        UVs[2] = new Vector2(textureRect.x, textureRect.y + textureRect.height);
        UVs[3] = new Vector2(textureRect.x, textureRect.y);

        meshData.uv.AddRange(UVs);

        meshData.colors.Add(color);
        meshData.colors.Add(color);
        meshData.colors.Add(color);
        meshData.colors.Add(color);

        if (world.config.useCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, localPos, globalPos, meshData, direction);
        }
    }

    public override string displayName
    {
        get
        {
            return base.displayName + " (" + color + ")";
        }
    }
}
