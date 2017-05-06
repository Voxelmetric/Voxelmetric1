using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

public class CubeBlock: Block
{
    public TextureCollection[] textures
    {
        get { return ((CubeBlockConfig)Config).textures; }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face)
    {
        bool backface = DirectionUtils.IsBackface(face.side);
        int d = DirectionUtils.Get(face.side);

        LocalPools pools = chunk.pools;
        VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
        {
            if (vertices==null)
            {
                Vector3 pos = face.pos;
                vertexData[0].Vertex = pos + BlockUtils.PaddingOffsets[d][0];
                vertexData[1].Vertex = pos + BlockUtils.PaddingOffsets[d][1];
                vertexData[2].Vertex = pos + BlockUtils.PaddingOffsets[d][2];
                vertexData[3].Vertex = pos + BlockUtils.PaddingOffsets[d][3];
            }
            else
            {
                vertexData[0].Vertex = vertices[0];
                vertexData[1].Vertex = vertices[1];
                vertexData[2].Vertex = vertices[2];
                vertexData[3].Vertex = vertices[3];
            }

            BlockUtils.PrepareTexture(chunk, ref face.pos, vertexData, face.side, textures);
            BlockUtils.PrepareColors(chunk, vertexData, face.side, ref face.light);
            
            chunk.GeometryHandler.Batcher.AddFace(vertexData, backface, face.materialID);
        }
        pools.VertexDataArrayPool.Push(vertexData);
    }
}
