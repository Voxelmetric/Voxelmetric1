using UnityEngine;
using System.Collections.Generic;
using System;
using System.Runtime.Serialization;

[Serializable]
public class CustomMeshBlock : Block {

    public CustomMeshBlockConfig customMeshConfig { get { return (CustomMeshBlockConfig)config; } }

    public CustomMeshBlock() { }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
            Rect texture;
            if (customMeshConfig.texture != null)
                texture = customMeshConfig.texture.GetTexture(chunk, localPos, globalPos, Direction.down);
            else
                texture = new Rect();

        meshData.AddMesh(customMeshConfig.tris, customMeshConfig.verts, customMeshConfig.uvs, texture, localPos);
    }

    // Constructor only used for deserialization
    protected CustomMeshBlock(SerializationInfo info, StreamingContext context):
        base(info, context) {
    }

}
