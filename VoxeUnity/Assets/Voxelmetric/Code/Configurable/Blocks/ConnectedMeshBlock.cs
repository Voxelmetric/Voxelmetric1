using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks;
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

    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face)
    {
        if (!connectedMeshConfig.directionalTris.ContainsKey(face.side))
            return;

        Rect texture;
        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        ChunkBlocks blocks = chunk.blocks;

        if (connectedMeshConfig.connectsToSolid && blocks.Get(face.pos + face.side).Solid)
        {
            texture = connectedMeshConfig.texture.GetTexture(chunk, face.pos, face.side);
            batcher.AddMeshData(connectedMeshConfig.directionalTris[face.side], connectedMeshConfig.directionalVerts[face.side], texture, face.pos, face.materialID);
        }
        else if (connectedMeshConfig.connectsToTypes.Length!=0)
        {
            int neighborType = blocks.Get(face.pos.Add(face.side)).Type;
            for (int i = 0; i<connectedMeshConfig.connectsToTypes.Length; i++)
            {
                if (neighborType==connectedMeshConfig.connectsToTypes[i])
                {
                    texture = connectedMeshConfig.texture.GetTexture(chunk, face.pos, face.side);
                    batcher.AddMeshData(connectedMeshConfig.directionalTris[face.side], connectedMeshConfig.directionalVerts[face.side], texture, face.pos, face.materialID);
                    break;
                }
            }
        }

        texture = customMeshConfig.texture.GetTexture(chunk, face.pos, Direction.down);
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, texture, face.pos, face.materialID);
    }

    public override void BuildBlock(Chunk chunk, Vector3Int localPos, int materialID)
    {
        for (int d = 0; d<6; d++)
        {
            Direction dir = DirectionUtils.Get(d);

            BlockFace face = new BlockFace()
            {
                block = null,
                pos = localPos,
                side = dir,
                light = new BlockLightData(0),
                materialID = materialID
            };

            BuildFace(chunk, null, ref face);
        }

        Rect texture = customMeshConfig.texture.GetTexture(chunk, localPos, Direction.down);
        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, texture, localPos, materialID);
    }
}