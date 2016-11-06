using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization;

[Serializable]
public class CubeBlock : SolidBlock {

    public TextureCollection[] textures { get { return ((CubeBlockConfig)config).textures; } }

    public CubeBlock() { }

    public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, localPos, globalPos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, localPos, globalPos, meshData, direction, textures);
        BlockBuilder.BuildColors(chunk, localPos, globalPos, meshData, direction);
    }

    // Constructor only used for deserialization
    protected CubeBlock(SerializationInfo info, StreamingContext context):
        base(info, context) {
    }
}
