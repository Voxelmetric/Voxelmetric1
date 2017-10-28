using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Load_Resources.Textures;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class CrossMeshBlock : Block
{
    private static readonly float coef = 1.0f / 64.0f;

    public TextureCollection texture { get { return ((CrossMeshBlockConfig)Config).texture; } }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        Custom = true;
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        LocalPools pools = chunk.pools;
        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;

        // Using the block positions hash is much better for random numbers than saving the offset and height in the block data
        int hash = localPos.GetHashCode();

        float blockHeight = (hash&63)*coef*Env.BlockSize;
        
        hash *= 39;
        float offsetX = (hash&63)*coef*Env.BlockSizeHalf-Env.BlockSizeHalf*0.5f;

        hash *= 39;
        float offsetZ = (hash&63)*coef*Env.BlockSizeHalf-Env.BlockSizeHalf*0.5f;

        // Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
        Vector3 vPos = localPos;
        vPos += new Vector3(offsetX, 0, offsetZ);

        float x1 = vPos.x-BlockUtils.blockPadding;
        float x2 = vPos.x+BlockUtils.blockPadding+Env.BlockSize;
        float y1 = vPos.y-BlockUtils.blockPadding;
        float y2 = vPos.y+BlockUtils.blockPadding+blockHeight;
        float z1 = vPos.z-BlockUtils.blockPadding;
        float z2 = vPos.z+BlockUtils.blockPadding+Env.BlockSize;

        var verts = pools.Vector3ArrayPool.PopExact(4);
        var uvs = pools.Vector2ArrayPool.PopExact(4);
        var colors = pools.Color32ArrayPool.PopExact(4);

        BlockUtils.PrepareTexture(chunk, ref localPos, uvs, Direction.north, texture, false);
        
        // TODO: How do I make sure that if I supply no color value, white is used?
        // TODO: These colors could be removed and memory would be saved
        {
            colors[0] = new Color32(255, 255, 255, 255);
            colors[1] = new Color32(255, 255, 255, 255);
            colors[2] = new Color32(255, 255, 255, 255);
            colors[3] = new Color32(255, 255, 255, 255);
        }

        {
            verts[0] = new Vector3(x1, y1, z2);
            verts[1] = new Vector3(x1, y2, z2);
            verts[2] = new Vector3(x2, y2, z1);
            verts[3] = new Vector3(x2, y1, z1);
            batcher.AddFace(materialID, verts, colors, uvs, false);
        }
        {
            verts[0] = new Vector3(x2, y1, z1);
            verts[1] = new Vector3(x2, y2, z1);
            verts[2] = new Vector3(x1, y2, z2);
            verts[3] = new Vector3(x1, y1, z2);
            batcher.AddFace(materialID, verts, colors, uvs, false);
        }
        {
            verts[0] = new Vector3(x2, y1, z2);
            verts[1] = new Vector3(x2, y2, z2);
            verts[2] = new Vector3(x1, y2, z1);
            verts[3] = new Vector3(x1, y1, z1);
            batcher.AddFace(materialID, verts, colors, uvs, false);
        }
        {
            verts[0] = new Vector3(x1, y1, z1);
            verts[1] = new Vector3(x1, y2, z1);
            verts[2] = new Vector3(x2, y2, z2);
            verts[3] = new Vector3(x2, y1, z2);
            batcher.AddFace(materialID, verts, colors, uvs, false);
        }

        pools.Color32ArrayPool.Push(colors);
        pools.Vector2ArrayPool.Push(uvs);
        pools.Vector3ArrayPool.Push(verts);
    }
}
