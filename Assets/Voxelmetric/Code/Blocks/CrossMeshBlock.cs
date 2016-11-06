using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization;

[Serializable]
public class CrossMeshBlock : Block
{
    public TextureCollection texture { get { return ((CrossMeshBlockConfig)config).texture; } }

    public CrossMeshBlock() { }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
        MeshBuilder.CrossMeshRenderer(chunk, localPos, globalPos, meshData, texture);
    }

    // Constructor only used for deserialization
    protected CrossMeshBlock(SerializationInfo info, StreamingContext context):
        base(info, context) {
    }
}
