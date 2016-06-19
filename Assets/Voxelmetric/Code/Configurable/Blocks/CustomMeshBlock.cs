using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;

public class CustomMeshBlock : Block {

    public CustomMeshBlockConfig customMeshConfig { get { return (CustomMeshBlockConfig)config; } }

    public override void BuildBlock(Chunk chunk, Vector3Int localPos, Vector3Int globalPos)
    {
        Rect texture;
        if (customMeshConfig.texture != null)
            texture = customMeshConfig.texture.GetTexture(chunk, localPos, globalPos, Direction.down);
        else
            texture = new Rect();

        DrawCallBatcher batcher = chunk.render.batcher;
        batcher.AddMeshData(customMeshConfig.tris, customMeshConfig.verts, texture, localPos);
    }

}
