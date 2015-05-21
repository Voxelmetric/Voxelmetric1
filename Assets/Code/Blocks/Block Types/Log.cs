using UnityEngine;
using System.Collections;

public class Log : BlockSolid
{
    public Log() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction, this);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, this, new Tile[] { Config.Textures.LogTop, Config.Textures.LogTop, Config.Textures.LogSide, Config.Textures.LogSide, Config.Textures.LogSide, Config.Textures.LogSide });
        BlockBuilder.BuildColors(chunk, pos, meshData, direction, this);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction, this);
        }
    }
}
