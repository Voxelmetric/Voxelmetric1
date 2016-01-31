using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;
using System;

[Serializable]
public class ConnectedMeshBlock : CustomMeshBlock
{
    public ConnectedMeshBlockConfig connectedMeshConfig { get { return (ConnectedMeshBlockConfig)config; } }

    public override void OnCreate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        if (connectedMeshConfig.connectsToTypes == null)
        {
            connectedMeshConfig.connectsToTypes = new int[connectedMeshConfig.connectsToNames.Length];
            for (int i = 0; i < connectedMeshConfig.connectsToNames.Length; i++)
            {
                connectedMeshConfig.connectsToTypes[i] = chunk.world.blockIndex.names[connectedMeshConfig.connectsToNames[i]];
            }
        }
    }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
        Rect texture;

        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            if (!connectedMeshConfig.directionalTris.ContainsKey(dir))
            {
                continue;
            }

            if (connectedMeshConfig.connectsToSolid && chunk.blocks.LocalGet(localPos + dir).IsSolid(DirectionUtils.Opposite(dir)))
            {
                texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, globalPos, dir);
                meshData.AddMesh(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir],
                    connectedMeshConfig.directionalUvs[dir], texture, localPos);
            }
            else if (connectedMeshConfig.connectsToTypes.Length != 0)
            {
                int neighborType = chunk.blocks.LocalGet(localPos.Add(dir)).type;
                for (int i = 0; i < connectedMeshConfig.connectsToTypes.Length; i++)
                {
                    if (neighborType == connectedMeshConfig.connectsToTypes[i])
                    {
                        texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, globalPos, dir);
                        meshData.AddMesh(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir],
                            connectedMeshConfig.directionalUvs[dir], texture, localPos);
                        break;
                    }
                }
            }

        }

        texture = customMeshConfig.texture.GetTexture(chunk, localPos, globalPos, Direction.down);
        meshData.AddMesh(customMeshConfig.tris, customMeshConfig.verts, customMeshConfig.uvs, texture, localPos);
    }

}