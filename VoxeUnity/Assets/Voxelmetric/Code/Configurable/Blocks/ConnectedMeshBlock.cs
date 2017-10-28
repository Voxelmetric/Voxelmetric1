using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class ConnectedMeshBlock: CustomMeshBlock
{
    public ConnectedMeshBlockConfig connectedMeshConfig
    {
        get { return (ConnectedMeshBlockConfig)Config; }
    }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        if (connectedMeshConfig.connectsToTypes==null)
        {
            connectedMeshConfig.connectsToTypes = new int[connectedMeshConfig.connectsToNames.Length];
            for (int i = 0; i<connectedMeshConfig.connectsToNames.Length; i++)
            {
                connectedMeshConfig.connectsToTypes[i] = blockProvider.GetType(connectedMeshConfig.connectsToNames[i]);
            }
        }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        var tris = connectedMeshConfig.directionalTris[(int)face.side];
        if (tris==null)
            return;

        var verts = connectedMeshConfig.directionalVerts[(int)face.side];
        var uvs = connectedMeshConfig.directionalUVs[(int)face.side];
        var texture = connectedMeshConfig.texture;

        Rect rect;
        ChunkBlocks blocks = chunk.blocks;

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;

        Vector3Int sidePos = face.pos.Add(face.side);
        if (connectedMeshConfig.connectsToSolid && blocks.Get(ref sidePos).Solid)
        {
            rect = connectedMeshConfig.texture.GetTexture(chunk, ref face.pos, face.side);
            batcher.AddMeshData(face.materialID, tris, verts, uvs, ref rect, face.pos);
        }
        else if (connectedMeshConfig.connectsToTypes.Length!=0)
        {
            int neighborType = blocks.Get(ref sidePos).Type;
            for (int i = 0; i<connectedMeshConfig.connectsToTypes.Length; i++)
            {
                if (neighborType==connectedMeshConfig.connectsToTypes[i])
                {
                    rect = texture.GetTexture(chunk, ref face.pos, face.side);
                    batcher.AddMeshData(face.materialID, tris, verts, uvs, ref rect, face.pos);
                    break;
                }
            }
        }

        rect = customMeshConfig.texture.GetTexture(chunk, ref face.pos, Direction.down);
        batcher.AddMeshData(face.materialID, customMeshConfig.tris, customMeshConfig.verts,  customMeshConfig.uvs, ref rect, face.pos);
    }

    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        for (int d = 0; d<6; d++)
        {
            Direction dir = DirectionUtils.Get(d);

            BlockFace face = new BlockFace
            {
                block = null,
                pos = localPos,
                side = dir,
                light = new BlockLightData(0),
                materialID = materialID
            };

            BuildFace(chunk, null, null, ref face, false);
        }

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;

        Rect texture = customMeshConfig.texture.GetTexture(chunk, ref localPos, Direction.down);
        batcher.AddMeshData(materialID, customMeshConfig.tris, customMeshConfig.verts, customMeshConfig.uvs, ref texture, localPos);
    }
}