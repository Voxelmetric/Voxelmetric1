using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

public class CubeBlock: SolidBlock
{

    public TextureCollection[] textures
    {
        get { return ((CubeBlockConfig)config).textures; }
    }

    public override void BuildFace(Chunk chunk, Vector3Int localPos, Vector3[] vertices, Direction direction)
    {
        bool backface = DirectionUtils.IsBackface(direction);

        VertexData[] vertexData = chunk.pools.VertexDataArrayPool.Pop(4);
        VertexDataFixed[] vertexDataFixed = chunk.pools.VertexDataFixedArrayPool.Pop(4);
        {
            if (vertices==null)
            {
                for (int i = 0; i<4; i++)
                    vertexData[i] = chunk.pools.VertexDataPool.Pop();
                BlockUtils.PrepareVertices(localPos, vertexData, direction);
            }
            else
            {
                for (int i = 0; i<4; i++)
                {
                    vertexData[i] = chunk.pools.VertexDataPool.Pop();
                    vertexData[i].Vertex = vertices[i];
                }
            }

            BlockUtils.PrepareTexture(chunk, localPos, vertexData, direction, textures);
            BlockUtils.PrepareColors(chunk, localPos, vertexData, direction);

            for (int i = 0; i < 4; i++)
                vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
            chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, backface);

            for (int i = 0; i < 4; i++)
                chunk.pools.VertexDataPool.Push(vertexData[i]);
        }
        chunk.pools.VertexDataFixedArrayPool.Push(vertexDataFixed);
        chunk.pools.VertexDataArrayPool.Push(vertexData);
    }
}
