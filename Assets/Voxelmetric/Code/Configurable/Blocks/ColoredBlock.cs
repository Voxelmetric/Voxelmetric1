using UnityEngine;
using System;
using Voxelmetric.Code.Blocks.Builders;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

[Serializable]
public class ColoredBlock : SolidBlock {

    public Color color;
    public TextureCollection texture { get { return ((ColoredBlockConfig)config).texture; } }

    protected override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, Direction direction)
    {
        VertexData[] vertexData = chunk.pools.PopVertexDataArray(4);
        {
            for (int i = 0; i < 4; i++)
                vertexData[i] = chunk.pools.PopVertexData();

            BlockBuilder.PrepareVertices(chunk, localPos, globalPos, vertexData, direction);
            BlockBuilder.PrepareTexture(chunk, localPos, globalPos, vertexData, direction, texture);
            BlockBuilder.SetColors(vertexData, ref color);
        }
        chunk.pools.PushVertexDataArray(vertexData);
    }

    public override string displayName
    {
        get
        {
            return base.displayName + " (" + color + ")";
        }
    }
}
