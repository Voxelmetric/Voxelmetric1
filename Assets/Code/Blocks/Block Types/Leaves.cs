using UnityEngine;
using System.Collections;

public class Leaves : BlockSolid
{
    public Leaves() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction, this);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, this, Config.Textures.Leaves);
        BlockBuilder.BuildColors(chunk, pos, meshData, direction, this);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction, this);
        }
    }

    public override bool IsSolid(Direction direction)
    {
        return false;
    }
}
