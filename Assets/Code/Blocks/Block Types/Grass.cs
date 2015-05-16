using UnityEngine;
using System.Collections;

public class Grass : BlockSolid
{
    public Grass() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction, this);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, this, new Tile[] { Textures.Grass, Textures.Dirt, Textures.GrassSide, Textures.GrassSide, Textures.GrassSide, Textures.GrassSide });
        BlockBuilder.BuildColors(chunk, pos, meshData, direction, this);
        BlockBuilder.BuildCollider(chunk, pos, meshData, direction, this);
    }
}
