using UnityEngine;
using System.Collections;

public class Dirt : BlockSolid {

    public Dirt() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, Config.Textures.Dirt);
        BlockBuilder.BuildColors(chunk, pos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction);
        }
    }
}
