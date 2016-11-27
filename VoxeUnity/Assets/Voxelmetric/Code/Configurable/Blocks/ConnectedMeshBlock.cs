using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Rendering.GeometryBatcher;

public class ConnectedMeshBlock: CustomMeshBlock
{
    public ConnectedMeshBlockConfig connectedMeshConfig
    {
        get { return (ConnectedMeshBlockConfig)config; }
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

    public override void BuildBlock(Chunk chunk, Vector3Int localPos)
    {
        Rect texture;
        RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;
        ChunkBlocks blocks = chunk.blocks;

        for (int d = 0; d<6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            if (!connectedMeshConfig.directionalTris.ContainsKey(dir))
                continue;

            if (connectedMeshConfig.connectsToSolid && blocks.GetBlock(localPos + dir).IsSolid(DirectionUtils.Opposite(dir)))
            {
                texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, dir);
                batcher.AddMeshData(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir], texture, localPos);
            }
            else if (connectedMeshConfig.connectsToTypes.Length!=0)
            {
                int neighborType = blocks.Get(localPos.Add(dir)).Type;
                for (int i = 0; i<connectedMeshConfig.connectsToTypes.Length; i++)
                {
                    if (neighborType==connectedMeshConfig.connectsToTypes[i])
                    {
                        texture = connectedMeshConfig.texture.GetTexture(chunk, localPos, dir);
                        batcher.AddMeshData(connectedMeshConfig.directionalTris[dir], connectedMeshConfig.directionalVerts[dir], texture, localPos);
                        break;
                    }
                }
            }
        }

        texture = customMeshConfig.texture.GetTexture(chunk, localPos, Direction.down);
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, texture, localPos);
    }
}