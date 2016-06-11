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
        VertexDataFixed[] vertexDataFixed = chunk.pools.PopVertexDataFixedArray(4);
        {
            for (int i = 0; i<4; i++)
                vertexData[i] = chunk.pools.PopVertexData();

            BlockBuilder.PrepareVertices(chunk, localPos, globalPos, vertexData, direction);
            BlockBuilder.PrepareTexture(chunk, localPos, globalPos, vertexData, direction, textures);
            BlockBuilder.PrepareColors(chunk, localPos, globalPos, vertexData, direction);

            for (int i = 0; i < 4; i++)
            {
                vertexDataFixed[i].Color = vertexData[i].Color;
                vertexDataFixed[i].Normal = vertexData[i].Normal;
                vertexDataFixed[i].Tangent = vertexData[i].Tangent;
                vertexDataFixed[i].Vertex = vertexData[i].Vertex;
                vertexDataFixed[i].UV = vertexData[i].UV;
            }
            chunk.render.batcher.AddFace(vertexDataFixed, DirectionUtils.Backface(direction));

            for (int i = 0; i < 4; i++)
                chunk.pools.PushVertexData(vertexData[i]);
        }
        chunk.pools.PushVertexDataFixedArray(vertexDataFixed);
        chunk.pools.PushVertexDataArray(vertexData);
    }

}
