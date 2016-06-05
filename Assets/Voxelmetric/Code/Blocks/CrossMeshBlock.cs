using System;

[Serializable]
public class CrossMeshBlock : Block
{
    public TextureCollection texture { get { return ((CrossMeshBlockConfig)config).texture; } }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
        MeshBuilder.CrossMeshRenderer(chunk, localPos, globalPos, meshData, texture);
    }
}
