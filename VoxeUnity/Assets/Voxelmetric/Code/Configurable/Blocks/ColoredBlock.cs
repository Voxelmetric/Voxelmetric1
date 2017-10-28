using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;

public class ColoredBlock : Block
{
    public Color32[] colors { get; private set; }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        colors = ((ColoredBlockConfig)Config).colors;
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        bool backFace = DirectionUtils.IsBackface(face.side);
        int d = DirectionUtils.Get(face.side);

        LocalPools pools = chunk.pools;
        var verts = pools.Vector3ArrayPool.PopExact(4);
        var cols = pools.Color32ArrayPool.PopExact(4);

        {
            if (vertices==null)
            {
                Vector3 pos = face.pos;

                verts[0] = pos+BlockUtils.PaddingOffsets[d][0];
                verts[1] = pos+BlockUtils.PaddingOffsets[d][1];
                verts[2] = pos+BlockUtils.PaddingOffsets[d][2];
                verts[3] = pos+BlockUtils.PaddingOffsets[d][3];

                cols[0] = colors[d];
                cols[1] = colors[d];
                cols[2] = colors[d];
                cols[3] = colors[d];
            }
            else
            {
                verts[0] = vertices[0];
                verts[1] = vertices[1];
                verts[2] = vertices[2];
                verts[3] = vertices[3];
                
                cols[0] = colors[d];
                cols[1] = colors[d];
                cols[2] = colors[d];
                cols[3] = colors[d];
            }

            BlockUtils.AdjustColors(chunk, cols, face.light);

            RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
            batcher.AddFace(face.materialID, verts, cols, backFace);
        }
        
        pools.Color32ArrayPool.Push(cols);
        pools.Vector3ArrayPool.Push(verts);
    }
}
