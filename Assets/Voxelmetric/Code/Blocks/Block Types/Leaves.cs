using UnityEngine;
using System.Collections;

public class Leaves : BlockSolid
{
    public Leaves() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, Config.Textures.Leaves);
        BlockBuilder.BuildColors(chunk, pos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction);
        }
    }

    public override bool IsSolid(Direction direction)
    {
        return false;
    }
    public override string Name() { return "leaves"; }
}
