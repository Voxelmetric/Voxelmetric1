using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Geometry.Batchers;
using Voxelmetric.Code.Load_Resources.Blocks;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

public class ConnectedMeshBlock: CustomMeshBlock
{
    public ConnectedMeshBlockConfig meshConfig
    {
        get { return (ConnectedMeshBlockConfig)Config; }
    }

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        if (meshConfig.connectsToTypes==null)
        {
            meshConfig.connectsToTypes = new int[meshConfig.connectsToNames.Length];
            for (int i = 0; i<meshConfig.connectsToNames.Length; i++)
            {
                meshConfig.connectsToTypes[i] = blockProvider.GetType(meshConfig.connectsToNames[i]);
            }
        }
    }

    public override void BuildFace(Chunk chunk, Vector3[] vertices, Color32[] palette, ref BlockFace face, bool rotated)
    {
        var d = meshConfig.dataDir[(int)face.side];

        var tris = d.tris;
        if (tris==null)
            return;

        var verts = d.verts;
        var uvs = d.uvs;
        var textures = d.textures;
        var colors = d.colors;

        Rect rect;
        ChunkBlocks blocks = chunk.blocks;

        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;

        Vector3Int sidePos = face.pos.Add(face.side);
        if (meshConfig.connectsToSolid && blocks.Get(ref sidePos).Solid)
        {
            rect = textures.GetTexture(chunk, ref face.pos, face.side);
            batcher.AddMeshData(face.materialID, tris, verts, colors, uvs, ref rect, face.pos);
        }
        else if (meshConfig.connectsToTypes.Length!=0)
        {
            int neighborType = blocks.Get(ref sidePos).Type;
            for (int i = 0; i<meshConfig.connectsToTypes.Length; i++)
            {
                if (neighborType==meshConfig.connectsToTypes[i])
                {
                    rect = textures.GetTexture(chunk, ref face.pos, face.side);
                    batcher.AddMeshData(face.materialID, tris, verts, colors, uvs, ref rect, face.pos);
                    break;
                }
            }
        }

        var d2 = meshConfig.data;
        rect = d2.textures.GetTexture(chunk, ref face.pos, Direction.down);
        batcher.AddMeshData(face.materialID, d2.tris, d2.verts, d2.colors, d2.uvs, ref rect, face.pos);
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

        var d2 = meshConfig.data;
        Rect texture = d2.textures.GetTexture(chunk, ref localPos, Direction.down);
        batcher.AddMeshData(materialID, d2.tris, d2.verts, d2.colors, d2.uvs, ref texture, localPos);
    }
}