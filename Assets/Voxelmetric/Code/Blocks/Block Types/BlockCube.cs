using UnityEngine;
using System.Collections;

public class BlockCube : BlockSolid {

    public string blockName;
    public TextureCollection[] textures;

    public override void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, localPos, globalPos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, localPos, globalPos, meshData, direction, textures);
        BlockBuilder.BuildColors(chunk, localPos, globalPos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, localPos, globalPos, meshData, direction);
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
