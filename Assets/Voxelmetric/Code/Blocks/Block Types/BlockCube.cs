using UnityEngine;
using System.Collections;

public class BlockCube : BlockSolid {

    public string blockName;
    public TextureCollection[] textures;

    public override void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, textures);
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
