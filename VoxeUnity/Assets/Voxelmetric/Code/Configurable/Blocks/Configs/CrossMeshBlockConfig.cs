using System.Collections;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlockConfig: BlockConfig
{
    public TextureCollection texture;

    public override bool SetUp(Hashtable config, World world)
    {
        if (!base.SetUp(config, world))
            return false;

        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));
        solid = _GetPropertyFromConfig(config, "solid", false);

        return true;
    }
}
