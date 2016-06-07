﻿using UnityEngine;
using System;
using Voxelmetric.Code.Blocks.Builders;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;

[Serializable]
public class ColoredBlock : SolidBlock {

    public Color color;
    public TextureCollection texture { get { return ((ColoredBlockConfig)config).texture; } }

    public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, localPos, globalPos, meshData, direction);

        Rect textureRect = texture.GetTexture(chunk, localPos, globalPos, direction);

        Vector2[] UVs = chunk.pools.PopVector2Array(4);
        UVs[0] = new Vector2(textureRect.x + textureRect.width, textureRect.y);
        UVs[1] = new Vector2(textureRect.x + textureRect.width, textureRect.y + textureRect.height);
        UVs[2] = new Vector2(textureRect.x, textureRect.y + textureRect.height);
        UVs[3] = new Vector2(textureRect.x, textureRect.y);
        for(int i=0;i<4; i++)
            meshData.uv.Add(UVs[i]);
        chunk.pools.PushVector2Array(UVs);

        meshData.colors.Add(color);
        meshData.colors.Add(color);
        meshData.colors.Add(color);
        meshData.colors.Add(color);
    }

    public override string displayName
    {
        get
        {
            return base.displayName + " (" + color + ")";
        }
    }
}