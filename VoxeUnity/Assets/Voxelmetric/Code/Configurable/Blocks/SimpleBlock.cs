using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;

public class SimpleBlock : Block
{
    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        bool backFace = DirectionUtils.IsBackface(face.side);

        LocalPools pools = chunk.pools;
        var verts = pools.Vector3ArrayPool.PopExact(4);
        var cols = pools.Color32ArrayPool.PopExact(4);

        {
            verts[0] = vertices[0];
            verts[1] = vertices[1];
            verts[2] = vertices[2];
            verts[3] = vertices[3];

            cols[0] = palette[face.block.Type & 255];
            cols[1] = palette[face.block.Type & 255];
            cols[2] = palette[face.block.Type & 255];
            cols[3] = palette[face.block.Type & 255];

            BlockUtils.AdjustColors(chunk, cols, face.light);

            RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
            batcher.AddFace(face.materialID, verts, cols, backFace);
        }

        pools.Color32ArrayPool.Push(cols);
        pools.Vector3ArrayPool.Push(verts);
    }
}
