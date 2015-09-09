using UnityEngine;
using System.Collections.Generic;
using Newtonsoft.Json;

/*
  This class acts like a middle man between the configuration files Voxelmetric understands
  and the configuration data that Voxelmetric components need. It reads JSON files from the
  resource folders and converts them into data structures that are stored for other classes
  to access.
*/
public class VoxelmetricConfigsLoader {

    Dictionary<string, TextureConfig> textureConfigs = new Dictionary<string, TextureConfig>();

    void LoadTextureConfigs()
    {
        var configFiles = Resources.LoadAll<TextAsset>(Config.Directories.TextureFolder);

        foreach (var configFile in configFiles)
        {
            TextureConfigArray configs = JsonConvert.DeserializeObject<TextureConfigArray>(configFile.text);
            foreach (var conf in configs.TextureConfigs)
            {
                textureConfigs.Add(conf.name, conf);
            }
        }
    }

    public TextureConfig GetTextureConfig(string textureName)
    {
        if (textureConfigs.Keys.Count == 0) {
            LoadTextureConfigs();
        }

        TextureConfig conf;
        if (textureConfigs.TryGetValue(textureName, out conf))
        {
            return conf;
        }
        else
        {
            Debug.LogError("Texture config not found for " + textureName + ". Using defaults");
            return conf;
        }
    }

    public TextureConfig[] AllTextureConfigs()
    {
        if (textureConfigs.Keys.Count == 0)
        {
            LoadTextureConfigs();
        }
        TextureConfig[] configs = new TextureConfig[textureConfigs.Count];
        textureConfigs.Values.CopyTo(configs, 0);
        return configs;
    }
	
}
