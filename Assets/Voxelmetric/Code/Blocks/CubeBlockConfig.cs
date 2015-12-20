using System;
using System.Collections;
using Newtonsoft.Json;

public class CubeBlockConfig: SolidBlockConfig
{
    public TextureCollection[] textures;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);

        textures = new TextureCollection[6];
        string[] textureNames = (string[])JsonConvert.DeserializeObject((string)config["textures"]);

        for (int i = 0; i < 6; i++)
        {
            textures[i] = world.textureIndex.GetTextureCollection(textureNames[i]);
        }
    }
}
