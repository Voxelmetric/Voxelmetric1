using UnityEngine;
using System.Collections;

public class CubeBlock : SolidBlock {

    public TextureCollection[] textures { get { return ((CubeBlockConfig)config).textures; } }

    public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, localPos, globalPos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, localPos, globalPos, meshData, direction, textures);
        BlockBuilder.BuildColors(chunk, localPos, globalPos, meshData, direction);
        if (world.config.useCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, localPos, globalPos, meshData, direction);
        }
    }

}
