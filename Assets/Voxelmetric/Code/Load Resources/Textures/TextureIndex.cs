using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TextureIndex {

    bool useTextureAtlas;
    string textureAtlasLocation;
    string textureFolder;

    public TextureIndex(string textureFolder, bool useTextureAtlas, string textureAtlasLocation)
    {
        this.textureFolder = textureFolder;
        this.useTextureAtlas = useTextureAtlas;
        this.textureAtlasLocation = textureAtlasLocation;
        LoadTextureIndex();
    }

    // Texture atlas
    public Dictionary<string, TextureCollection> textures = new Dictionary<string, TextureCollection>();

    public Texture2D atlas;

    void LoadTextureIndex()
    {
        // If you're using a pre defined texture atlas return now, don't try to generate a new one
        if (useTextureAtlas)
        {
            UseCustomTextureAtlas();
            return;
        }

        TextureConfig[] configs = LoadAllTextures();
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
        Texture2D packedTextures = new Texture2D(8192, 8192);
        Rect[] rects = packedTextures.PackTextures(individualTextures.ToArray(), Config.Env.textureAtlasPadding, 8192, false);

        // Transfer over the pixels to another texture2d because PackTextures resets the texture format and useMipMaps settings
        atlas = new Texture2D(packedTextures.width, packedTextures.height, Config.Env.textureFormat, Config.Env.useMipMaps);
        atlas.SetPixels(packedTextures.GetPixels(0, 0, packedTextures.width, packedTextures.height));
        atlas.filterMode = Config.Env.textureAtlasFiltering;

        List<Rect> repeatingTextures = new List<Rect>();
        List<Rect> nonrepeatingTextures = new List<Rect>();

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
                    repeatingTextures.Add(rects[index]);
                }
                else
                {
                    nonrepeatingTextures.Add(rects[index]);
                }
                index++;
            }
        }

        uPaddingBleed.BleedEdges(atlas, Config.Env.textureAtlasPadding, repeatingTextures.ToArray(), repeatingTextures: true);
        uPaddingBleed.BleedEdges(atlas, Config.Env.textureAtlasPadding, nonrepeatingTextures.ToArray(), repeatingTextures: false);
    }

    //This function is used if you've made your own texture atlas and the configs just specify where the textures are
    void UseCustomTextureAtlas()
    {
        atlas = Resources.Load<Texture2D>(textureAtlasLocation);
        TextureConfig[] configs = new ConfigLoader<TextureConfig>(new string[] { textureFolder }).AllConfigs();

        for (int i = 0; i < configs.Length; i++)
        {
            for (int j = 0; j < configs[i].textures.Length; j++)
            {
                Rect texture = new Rect(configs[i].textures[j].xPos / (float)atlas.width, configs[i].textures[j].yPos/ (float)atlas.height,
                    configs[i].textures[j].width/ (float)atlas.width, configs[i].textures[j].height/ (float)atlas.height);

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
            }
        }
    }

    TextureConfig[] LoadAllTextures()
    {
        TextureConfig[] configs = new ConfigLoader<TextureConfig>(new string[] { textureFolder }).AllConfigs();

        // Load all files in Textures folder
        Texture2D[] sourceTextures = Resources.LoadAll<Texture2D>(textureFolder);

        Dictionary<string, Texture2D> sourceTexturesLookup = new Dictionary<string, Texture2D>();
        foreach (var texture in sourceTextures) {
            sourceTexturesLookup.Add(texture.name, texture);
        }

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
        else //If theres a width and a height fetch the pixels specified by the rect as a texture
        {
            Texture2D newTexture = new Texture2D(texture.width, texture.height, Config.Env.textureFormat, file.mipmapCount < 1);
            newTexture.SetPixels(0, 0, texture.width, texture.height, file.GetPixels(texture.xPos, texture.yPos, texture.width, texture.height));
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
