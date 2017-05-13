using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;

public class ColoredBlock : Block
{
    public Color32[] colors
    {
        get { return ((ColoredBlockConfig)Config).colors; }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face, bool rotated)
    {
        bool backFace = DirectionUtils.IsBackface(face.side);
        int d = DirectionUtils.Get(face.side);

        LocalPools pools = chunk.pools;
        VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
        {
            if (vertices==null)
            {
                Vector3 pos = face.pos;

                vertexData[0].Vertex = pos+BlockUtils.PaddingOffsets[d][0];
                vertexData[0].Color = colors[d];
                vertexData[0].UV = Vector2.zero;

                vertexData[1].Vertex = pos+BlockUtils.PaddingOffsets[d][1];
                vertexData[1].Color = colors[d];
                vertexData[1].UV = Vector2.zero;

                vertexData[2].Vertex = pos+BlockUtils.PaddingOffsets[d][2];
                vertexData[2].Color = colors[d];
                vertexData[2].UV = Vector2.zero;

                vertexData[3].Vertex = pos+BlockUtils.PaddingOffsets[d][3];
                vertexData[3].Color = colors[d];
                vertexData[3].UV = Vector2.zero;
            }
            else
            {
                vertexData[0].Vertex = vertices[0];
                vertexData[0].Color = colors[d];
                vertexData[0].UV = Vector2.zero;

                vertexData[1].Vertex = vertices[1];
                vertexData[1].Color = colors[d];
                vertexData[1].UV = Vector2.zero;

                vertexData[2].Vertex = vertices[2];
                vertexData[2].Color = colors[d];
                vertexData[2].UV = Vector2.zero;

                vertexData[3].Vertex = vertices[3];
                vertexData[3].Color = colors[d];
                vertexData[3].UV = Vector2.zero;
            }

            BlockUtils.AdjustColors(chunk, vertexData, face.side, face.light);

            chunk.GeometryHandler.Batcher.AddFace(vertexData, backFace, face.materialID);
        }
        pools.VertexDataArrayPool.Push(vertexData);
    }
}
