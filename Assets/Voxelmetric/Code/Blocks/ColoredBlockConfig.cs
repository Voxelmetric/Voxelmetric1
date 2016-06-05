using System.Collections;

public class ColoredBlockConfig : SolidBlockConfig
{
    public TextureCollection texture;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);
        texture = world.textureIndex.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));
    }
}
