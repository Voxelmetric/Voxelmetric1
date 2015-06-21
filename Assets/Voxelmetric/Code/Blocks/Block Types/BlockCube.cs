using UnityEngine;
using System.Collections;

public class BlockCube : BlockSolid {

    public string blockName;
    public Vector2[] textures;
    public bool isSolid = true;

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, new Tile[] {
            new Tile((int)textures[0].x, (int)textures[0].y),
            new Tile((int)textures[1].x, (int)textures[1].y),
            new Tile((int)textures[2].x, (int)textures[2].y),
            new Tile((int)textures[3].x, (int)textures[3].y),
            new Tile((int)textures[4].x, (int)textures[4].y),
            new Tile((int)textures[5].x, (int)textures[5].y)
        });
        BlockBuilder.BuildColors(chunk, pos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction);
        }
    }

    public override string Name()
    {
        return blockName;
    }

    public override bool IsSolid(Direction direction)
    {
        return isSolid;
    }

}
