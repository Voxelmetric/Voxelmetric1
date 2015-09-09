using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TextureIndex {

    // Texture atlas
    public Dictionary<string, TextureCollection> textures = new Dictionary<string, TextureCollection>();

    public Texture2D atlas;

    void LoadTextureIndex()
    {
       
        TextureConfig[] configs = LoadAllTextures();

        // Create new atlas
        atlas = new Texture2D(8192, 8192);
        atlas.filterMode = Config.Env.textureAtlasFiltering;

        List<Texture2D> individualTextures = new List<Texture2D>();
        for (int i = 0; i < configs.Length; i++)
        {
            for (int j = 0; j < configs[i].textures.Length; j++)
            {
                //create an array of all these textures
                individualTextures.Add(configs[i].textures[j].texture2d);
            }
        }

        // Generate atlas
        Rect[] rects = atlas.PackTextures(individualTextures.ToArray(), Config.Env.textureAtlasPadding, 8192, false);

        List<int> repeatingTextures = new List<int>();
        List<int> nonrepeatingTextures = new List<int>();

        int index = 0;
        for (int i = 0; i < configs.Length; i++)
        {
            for (int j = 0; j < configs[i].textures.Length; j++)
            {
                Rect texture = rects[index];

                TextureCollection collection;
                if (!textures.TryGetValue(configs[i].name, out collection))
                {
                    collection = new TextureCollection(configs[i].name);
                    textures.Add(configs[i].name, collection);
                }

                int connectedTextureType = -1;
                if (configs[i].connectedTextures)
                {
                    connectedTextureType = configs[i].textures[j].connectedType;
                }


                collection.AddTexture(texture, connectedTextureType, configs[i].textures[j].weight);

                if (configs[i].textures[j].repeatingTexture)
                {
                    repeatingTextures.Add(index);
                }
                else
                {
                    nonrepeatingTextures.Add(index);
                }
                index++;
            }
        }
        uPaddingBleed.BleedEdges(atlas, Config.Env.textureAtlasPadding, rects, repeatingTextures: true);

    }

    TextureConfig[] LoadAllTextures()
    {
        // Load all files in Textures folder
        Texture2D[] sourceTextures = Resources.LoadAll<Texture2D>(Config.Directories.TextureFolder);

        Dictionary<string, Texture2D> sourceTexturesLookup = new Dictionary<string, Texture2D>();
        foreach (var texture in sourceTextures) {
            sourceTexturesLookup.Add(texture.name, texture);
        }

        TextureConfig[] configs = Voxelmetric.resources.config.AllTextureConfigs();
        for (int i = 0; i < configs.Length; i++)
        {
            for (int n = 0; n < configs[i].textures.Length; n++)
            {
                configs[i].textures[n].texture2d = Texture2DFromConfig(configs[i].textures[n], sourceTexturesLookup);
            }

            if (configs[i].connectedTextures) {
                //Create all 48 possibilities from the 5 supplied textures
                Texture2D[] newTextures = ConnectedTextures.ConnectedTexturesFromBaseTextures(configs[i].textures);
                TextureConfig.Texture[] connectedTextures = new TextureConfig.Texture[48];

                for (int x = 0; x < newTextures.Length; x++)
                {
                    connectedTextures[x].connectedType = x;
                    connectedTextures[x].texture2d = newTextures[x];
                }

                configs[i].textures = connectedTextures;
            }
        }

        return configs;
    }

    Texture2D Texture2DFromConfig(TextureConfig.Texture texture, Dictionary<string, Texture2D> sourceTexturesLookup) {
        Texture2D file;
        if (!sourceTexturesLookup.TryGetValue(texture.file, out file)){
            Debug.LogError("Config referred to nonexistent file: " + texture.file);
            return null;
        }

        //No width or height means this texture is the whole file
        if (texture.width == 0 && texture.height == 0)
        {
            return file;
        }
        else
        {
            Texture2D newTexture = new Texture2D(texture.width, texture.height);
            newTexture.SetPixels(0, 0, texture.width, texture.height, file.GetPixels(0, 0, texture.width, texture.height));
            return newTexture;
        }
    }

    public TextureCollection GetTextureCollection(string textureName)
    {
        if (textures.Keys.Count == 0)
        {
            LoadTextureIndex();
        }

        TextureCollection collection;

        textures.TryGetValue(textureName, out collection);

        return collection;
    }

}
