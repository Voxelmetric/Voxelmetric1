using System.Collections;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class ColoredBlockConfig : SolidBlockConfig
{
    public TextureCollection texture;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);
        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));
    }
}
