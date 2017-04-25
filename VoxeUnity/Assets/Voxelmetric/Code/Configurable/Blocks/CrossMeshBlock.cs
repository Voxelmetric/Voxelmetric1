using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlock : Block
{
    public TextureCollection texture { get { return ((CrossMeshBlockConfig)Config).texture; } }
    
    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        MeshUtils.BuildCrossMesh(chunk, localPos, texture, true, materialID);
    }
}
