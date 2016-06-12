using System;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;

[Serializable]
public class CrossMeshBlock : Block
{
    public TextureCollection texture { get { return ((CrossMeshBlockConfig)config).texture; } }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        MeshUtils.BuildCrossMesh(chunk, localPos, globalPos, texture);
    }
}
