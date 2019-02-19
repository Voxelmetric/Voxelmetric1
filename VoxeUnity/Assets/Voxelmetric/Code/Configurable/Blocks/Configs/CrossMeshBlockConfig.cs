using System.Collections;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CrossMeshBlockConfig: BlockConfig
{
    public TextureCollection texture;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;

        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        return true;
    }
}
