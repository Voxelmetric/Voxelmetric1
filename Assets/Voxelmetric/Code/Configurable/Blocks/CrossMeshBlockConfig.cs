using System.Collections;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlockConfig: BlockConfig
{
    public TextureCollection texture;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);
        texture = world.textureIndex.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));
        solid = _GetPropertyFromConfig(config, "solid", false);
    }
}
