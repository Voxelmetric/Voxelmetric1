using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Geometry.GeometryBatcher;

public class ConnectedMeshBlock: CustomMeshBlock
{
    public ConnectedMeshBlockConfig connectedMeshConfig
    {
        get { return (ConnectedMeshBlockConfig)Config; }
    }

    public override void OnInit(BlockProvider blockProvider)
    {
        Custom = true;

        if (connectedMeshConfig.connectsToTypes==null)
        {
            connectedMeshConfig.connectsToTypes = new int[connectedMeshConfig.connectsToNames.Length];
            for (int i = 0; i<connectedMeshConfig.connectsToNames.Length; i++)
            {
                connectedMeshConfig.connectsToTypes[i] = blockProvider.GetType(connectedMeshConfig.connectsToNames[i]);
            }
        }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face, bool rotated)
    {
        var tris = connectedMeshConfig.directionalTris[(int)face.side];
        if (tris==null)
            return;

        var verts = connectedMeshConfig.directionalVerts[(int)face.side];
        var texture = connectedMeshConfig.texture;

        Rect rect;
        ChunkBlocks blocks = chunk.blocks;

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        batcher.UseTextures(face.materialID);

        Vector3Int sidePos = face.pos.Add(face.side);
        if (connectedMeshConfig.connectsToSolid && blocks.Get(ref sidePos).Solid)
        {
            rect = connectedMeshConfig.texture.GetTexture(chunk, ref face.pos, face.side);
            batcher.AddMeshData(tris, verts, ref rect, face.pos, face.materialID);
        }
        else if (connectedMeshConfig.connectsToTypes.Length!=0)
        {
            int neighborType = blocks.Get(ref sidePos).Type;
            for (int i = 0; i<connectedMeshConfig.connectsToTypes.Length; i++)
            {
                if (neighborType==connectedMeshConfig.connectsToTypes[i])
                {
                    rect = texture.GetTexture(chunk, ref face.pos, face.side);
                    batcher.AddMeshData(tris, verts, ref rect, face.pos, face.materialID);
                    break;
                }
            }
        }

        rect = customMeshConfig.texture.GetTexture(chunk, ref face.pos, Direction.down);
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, ref rect, face.pos, face.materialID);
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
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

            BuildFace(chunk, null, ref face, false);
        }

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        batcher.UseTextures(materialID);

        Rect texture = customMeshConfig.texture.GetTexture(chunk, ref localPos, Direction.down);
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, ref texture, localPos, materialID);
    }
}