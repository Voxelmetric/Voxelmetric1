using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

public class ColoredBlock : SolidBlock {

    public Color color;
    public TextureCollection texture { get { return ((ColoredBlockConfig)Config).texture; } }

    public override void BuildFace(Chunk chunk, Vector3Int localPos, Vector3[] vertices, Direction direction)
    {
        bool backFace = DirectionUtils.IsBackface(direction);

        VertexData[] vertexData = chunk.pools.VertexDataArrayPool.Pop(4);
        VertexDataFixed[] vertexDataFixed = chunk.pools.VertexDataFixedArrayPool.Pop(4);
        {
            if (vertices == null)
            {
                for (int i = 0; i < 4; i++)
                    vertexData[i] = chunk.pools.VertexDataPool.Pop();
                BlockUtils.PrepareVertices(localPos, vertexData, direction);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    vertexData[i] = chunk.pools.VertexDataPool.Pop();
                    vertexData[i].Vertex = vertices[i];
                }
            }

            BlockUtils.PrepareTexture(chunk, localPos, vertexData, direction, texture);
            BlockUtils.SetColors(vertexData, ref color);

            for (int i = 0; i<4; i++)
                vertexDataFixed[i]= VertexDataUtils.ClassToStruct(vertexData[i]);
            chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, backFace);

            for (int i = 0; i < 4; i++)
                chunk.pools.VertexDataPool.Push(vertexData[i]);
        }
        chunk.pools.VertexDataFixedArrayPool.Push(vertexDataFixed);
        chunk.pools.VertexDataArrayPool.Push(vertexData);
    }

    public override string DisplayName
    {
        get
        {
            return base.DisplayName + " (" + color + ")";
        }
    }
}
