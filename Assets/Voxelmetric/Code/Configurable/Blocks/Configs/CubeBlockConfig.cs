using System.Collections;
using Newtonsoft.Json;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CubeBlockConfig: SolidBlockConfig
{
    public TextureCollection[] textures;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);

        textures = new TextureCollection[6];
        Newtonsoft.Json.Linq.JArray textureNames = (Newtonsoft.Json.Linq.JArray)JsonConvert.DeserializeObject(config["textures"].ToString());

        for (int i = 0; i < 6; i++)
        {
            textures[i] = world.textureProvider.GetTextureCollection(textureNames[i].ToObject<string>());
        }
    }
}
