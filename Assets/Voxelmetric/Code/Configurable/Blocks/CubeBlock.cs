using System;
using Voxelmetric.Code.Blocks.Builders;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

[Serializable]
public class CubeBlock : SolidBlock {

    public TextureCollection[] textures { get { return ((CubeBlockConfig)config).textures; } }

    protected override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, Direction direction)
    {
        VertexData[] vertexData = chunk.pools.PopVertexDataArray(4);
        {
            for (int i = 0; i<4; i++)
                vertexData[i] = chunk.pools.PopVertexData();

            BlockBuilder.PrepareVertices(chunk, localPos, globalPos, vertexData, direction);
            BlockBuilder.PrepareTexture(chunk, localPos, globalPos, vertexData, direction, textures);
            BlockBuilder.PrepareColors(chunk, localPos, globalPos, vertexData, direction);
        }
        chunk.pools.PushVertexDataArray(vertexData);
    }

}
