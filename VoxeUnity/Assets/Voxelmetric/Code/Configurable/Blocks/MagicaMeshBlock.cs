using System;
using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry;
using Voxelmetric.Code.Geometry.GeometryBatcher;

public class MagicaMeshBlock : Block
{
    public MagicaMeshBlockConfig magicMeshConfig { get { return (MagicaMeshBlockConfig)Config; } }
    
    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face, bool rotated)
    {
        bool backFace = DirectionUtils.IsBackface(face.side);
        
        LocalPools pools = chunk.pools;
        VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
        {
            vertexData[0].Vertex = vertices[0];
            vertexData[0].Color = Color.white;
            vertexData[0].UV = Vector2.zero;

            vertexData[1].Vertex = vertices[1];
            vertexData[1].Color = Color.white;
            vertexData[1].UV = Vector2.zero;

            vertexData[2].Vertex = vertices[2];
            vertexData[2].Color = Color.white;
            vertexData[2].UV = Vector2.zero;

            vertexData[3].Vertex = vertices[3];
            vertexData[3].Color = Color.white;
            vertexData[3].UV = Vector2.zero;

            BlockUtils.AdjustColors(chunk, vertexData, face.light);
            magicMeshConfig.AddFace(vertexData, backFace);
        }
        pools.VertexDataArrayPool.Push(vertexData);
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        Rect rect = new Rect();

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        batcher.UseColors(materialID);

        batcher.AddMeshData(magicMeshConfig.tris, magicMeshConfig.verts, ref rect, localPos, materialID);
    }
}
