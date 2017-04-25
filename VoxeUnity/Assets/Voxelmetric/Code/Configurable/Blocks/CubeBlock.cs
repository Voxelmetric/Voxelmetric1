using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.GeometryHandler;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Rendering.GeometryBatcher;

public class CubeBlock: SolidBlock
{

    public TextureCollection[] textures
    {
        get { return ((CubeBlockConfig)Config).textures; }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face)
    {
        bool backface = DirectionUtils.IsBackface(face.side);

        LocalPools pools = chunk.pools;
        VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
        VertexDataFixed[] vertexDataFixed = pools.VertexDataFixedArrayPool.PopExact(4);
        {
            if (vertices==null)
            {
                for (int i = 0; i<4; i++)
                    vertexData[i] = pools.VertexDataPool.Pop();
                BlockUtils.PrepareVertices(ref face.pos, vertexData, face.side);
            }
            else
            {
                for (int i = 0; i<4; i++)
                {
                    vertexData[i] = pools.VertexDataPool.Pop();
                    vertexData[i].Vertex = vertices[i];
                }
            }

            BlockUtils.PrepareTexture(chunk, ref face.pos, vertexData, face.side, textures);
            BlockUtils.PrepareColors(chunk, vertexData, face.side, ref face.light);

            for (int i = 0; i < 4; i++)
                vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
            chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, backface, face.materialID);

            for (int i = 0; i < 4; i++)
                pools.VertexDataPool.Push(vertexData[i]);
        }
        pools.VertexDataFixedArrayPool.Push(vertexDataFixed);
        pools.VertexDataArrayPool.Push(vertexData);
    }
}
