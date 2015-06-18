using UnityEngine;
using System.Collections;

public class Grass : BlockSolid
{
    public Grass() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, new Tile[] { Config.Textures.Grass, Config.Textures.Dirt, Config.Textures.GrassSide, Config.Textures.GrassSide, Config.Textures.GrassSide, Config.Textures.GrassSide });
        BlockBuilder.BuildColors(chunk, pos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction);
        }
    }
}
