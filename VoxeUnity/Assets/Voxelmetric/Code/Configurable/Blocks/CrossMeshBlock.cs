using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlock : Block
{
    public TextureCollection texture { get { return ((CrossMeshBlockConfig)config).texture; } }

    public override void BuildBlock(Chunk chunk, Vector3Int localPos)
    {
        MeshUtils.BuildCrossMesh(chunk, localPos, texture);
    }
}
