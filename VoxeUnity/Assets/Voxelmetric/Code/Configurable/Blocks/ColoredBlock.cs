using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;

public class ColoredBlock : SolidBlock
{
    public Color[] colors
    {
        get { return ((ColoredBlockConfig)Config).colors; }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face)
    {
        bool backFace = DirectionUtils.IsBackface(face.side);

        LocalPools pools = chunk.pools;
        VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
        VertexDataFixed[] vertexDataFixed = pools.VertexDataFixedArrayPool.PopExact(4);
        {
            if (vertices == null)
            {
                for (int i = 0; i<4; i++)
                {
                    vertexData[i] = pools.VertexDataPool.Pop();
                    vertexData[i].Color = colors[(int)face.side];
                    vertexData[i].UV = Vector2.zero;
                }
                BlockUtils.PrepareVertices(ref face.pos, vertexData, face.side);
            }
            else
            {
                for (int i = 0; i < 4; i++)
                {
                    vertexData[i] = pools.VertexDataPool.Pop();
                    vertexData[i].Vertex = vertices[i];
                    vertexData[i].Color = colors[(int)face.side];
                    vertexData[i].UV = Vector2.zero;
                }
            }
            
            BlockUtils.AdjustColors(chunk, vertexData, face.side, face.light);

            for (int i = 0; i<4; i++)
                vertexDataFixed[i]= VertexDataUtils.ClassToStruct(vertexData[i]);
            chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, backFace, face.materialID);

            for (int i = 0; i < 4; i++)
                pools.VertexDataPool.Push(vertexData[i]);
        }
        pools.VertexDataFixedArrayPool.Push(vertexDataFixed);
        pools.VertexDataArrayPool.Push(vertexData);
    }
}
