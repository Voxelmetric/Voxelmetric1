using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class TextureIndex {

    // Texture atlas
    public Dictionary<string, TextureCollection> textures = new Dictionary<string, TextureCollection>();

    public Texture2D atlas;

    public TextureIndex(bool loadEmpty = false)
    {
        if (loadEmpty)
            return;

        List<Texture2D> atlasTextures = new List<Texture2D>();

        // Load all files in Textures folder
        atlasTextures.AddRange(Resources.LoadAll<Texture2D>(Config.Directories.TextureFolder));
        
        //Load and generate all the connected textures
        List<Texture2D> connectedTextures = new List<Texture2D>();
        connectedTextures.AddRange(Resources.LoadAll<Texture2D>(Config.Directories.ConnectedTextureFolder));

        if (connectedTextures.Count!=0)
            atlasTextures.AddRange(ConnectedTextures.GenerateConnectedTextures(connectedTextures));

        // Create new atlas
        atlas = new Texture2D(8192, 8192);
        atlas.filterMode = FilterMode.Point;

        // Generate atlas
        Rect[] rects = atlas.PackTextures(atlasTextures.ToArray(), 0, 8192, false);

        // Add each atlas entry into texture dictionary
        for (int i = 0; i < atlasTextures.Count; i++)
        {
            if (!atlasTextures[i])
                continue;

            string[] fileName = atlasTextures[i].name.ToString().Split('-');
            Rect texture = rects[i];

            string textureName = fileName[0];
            int connectedTextureType = -1;

            //Get the properties associated with this texture
            for (int n = 1; n < fileName.Length; n++)
            {
                switch (fileName[n][0])
                {
                    case 'c':
                        int.TryParse(fileName[n].Substring(1), out connectedTextureType);
                        break;
                    default:
                        break;
                }
            }

            //Create a texture collection with this name if it doesn't exist already
            TextureCollection collection;
            if (!textures.TryGetValue(textureName, out collection))
            {
                collection = new TextureCollection(textureName);
                textures.Add(textureName, collection);
            }

            collection.AddTexture(texture, connectedTextureType);

        }

    }

    public TextureCollection GetTextureCollection(string textureName)
    {
        TextureCollection collection;

        textures.TryGetValue(textureName, out collection);

        return collection;
    }

}
