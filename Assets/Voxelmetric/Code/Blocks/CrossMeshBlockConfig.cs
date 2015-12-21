using System;
using System.Collections;

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
