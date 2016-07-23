using System.Collections;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class ColoredBlockConfig : SolidBlockConfig
{
    public TextureCollection texture;

    public override bool SetUp(Hashtable config, World world)
    {
        if (!base.SetUp(config, world))
            return false;

        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        return true;
    }
}
