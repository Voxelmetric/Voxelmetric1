using UnityEngine;
using System;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;

[Serializable]
public class ConnectedMeshBlock: CustomMeshBlock
{
    public ConnectedMeshBlockConfig connectedMeshConfig
    {
        get { return (ConnectedMeshBlockConfig)config; }
    }

    public override void OnCreate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        if (connectedMeshConfig.connectsToTypes==null)
        {
            connectedMeshConfig.connectsToTypes = new int[connectedMeshConfig.connectsToNames.Length];
            for (int i = 0; i<connectedMeshConfig.connectsToNames.Length; i++)
            {
                connectedMeshConfig.connectsToTypes[i] = chunk.world.blockIndex.names[connectedMeshConfig.connectsToNames[i]];
            }
        }
    }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        Rect texture;
        DrawCallBatcher batcher = chunk.render.batcher;

        for (int d = 0; d<6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            if (!connectedMeshConfig.directionalTris.ContainsKey(dir))
                continue;

            if (connectedMeshConfig.connectsToSolid &&
                chunk.blocks.LocalGet(localPos+dir).IsSolid(DirectionUtils.Opposite(dir)))
            {
                texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, globalPos, dir);
                batcher.BuildMesh(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir], texture);
            }
            else if (connectedMeshConfig.connectsToTypes.Length!=0)
            {
                int neighborType = chunk.blocks.LocalGet(localPos.Add(dir)).type;
                for (int i = 0; i<connectedMeshConfig.connectsToTypes.Length; i++)
                {
                    if (neighborType==connectedMeshConfig.connectsToTypes[i])
                    {
                        texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, globalPos, dir);
                        batcher.BuildMesh(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir], texture);
                        break;
                    }
                }
            }
        }

        texture = customMeshConfig.texture.GetTexture(chunk, localPos, globalPos, Direction.down);
        batcher.BuildMesh(customMeshConfig.tris, customMeshConfig.verts, texture);
    }
}