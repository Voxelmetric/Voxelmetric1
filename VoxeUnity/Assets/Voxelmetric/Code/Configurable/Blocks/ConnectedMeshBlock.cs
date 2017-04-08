using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Rendering.GeometryBatcher;

public class ConnectedMeshBlock: CustomMeshBlock
{
    public ConnectedMeshBlockConfig connectedMeshConfig
    {
        get { return (ConnectedMeshBlockConfig)Config; }
    }

    public override void OnInit(BlockProvider blockProvider)
    {
        if (connectedMeshConfig.connectsToTypes==null)
        {
            connectedMeshConfig.connectsToTypes = new int[connectedMeshConfig.connectsToNames.Length];
            for (int i = 0; i<connectedMeshConfig.connectsToNames.Length; i++)
            {
                connectedMeshConfig.connectsToTypes[i] = blockProvider.GetType(connectedMeshConfig.connectsToNames[i]);
            }
        }
    }

    public override void BuildFace(Chunk chunk, Vector3Int localPos, Vector3[] vertices, Direction dir, int materialID)
    {
        if (!connectedMeshConfig.directionalTris.ContainsKey(dir))
            return;

        Rect texture;
        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        ChunkBlocks blocks = chunk.blocks;

        if (connectedMeshConfig.connectsToSolid && blocks.Get(localPos + dir).Solid)
        {
            texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, dir);
            batcher.AddMeshData(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir], texture, localPos, materialID);
        }
        else if (connectedMeshConfig.connectsToTypes.Length!=0)
        {
            int neighborType = blocks.Get(localPos.Add(dir)).Type;
            for (int i = 0; i<connectedMeshConfig.connectsToTypes.Length; i++)
            {
                if (neighborType==connectedMeshConfig.connectsToTypes[i])
                {
                    texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, dir);
                    batcher.AddMeshData(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir], texture, localPos, materialID);
                    break;
                }
            }
        }

        texture = customMeshConfig.texture.GetTexture(chunk, localPos, Direction.down);
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, texture, localPos, materialID);
    }

    public override void BuildBlock(Chunk chunk, Vector3Int localPos, int materialID)
    {
        for (int d = 0; d<6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            BuildFace(chunk, localPos, null, dir, materialID);
        }

        Rect texture = customMeshConfig.texture.GetTexture(chunk, localPos, Direction.down);
        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, texture, localPos, materialID);
    }
}