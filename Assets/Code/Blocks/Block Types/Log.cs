using UnityEngine;
using System.Collections;

public class Log : BlockSolid
{

    public static int health = 100;
    public static int toughness = 50;
    public static bool canBeWalkedOn = true;

    public Log() : base() { }

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction, this);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, this, new Tile[] { Textures.LogTop, Textures.LogTop, Textures.LogSide, Textures.LogSide, Textures.LogSide, Textures.LogSide });
        BlockBuilder.BuildColors(chunk, pos, meshData, direction, this);
        BlockBuilder.BuildCollider(chunk, pos, meshData, direction, this);
    }
}
