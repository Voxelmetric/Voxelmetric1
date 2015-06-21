using UnityEngine;
using System.Collections;

public class Log : BlockSolid
{
    public Log() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, new Tile[] { Config.Textures.LogTop, Config.Textures.LogTop, Config.Textures.LogSide, Config.Textures.LogSide, Config.Textures.LogSide, Config.Textures.LogSide });
        BlockBuilder.BuildColors(chunk, pos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction);
        }
    }
    public override string Name() { return "log"; }
}
