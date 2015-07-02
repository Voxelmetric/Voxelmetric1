using UnityEngine;
using System.Collections.Generic;

public class TextureCollection {

    public string textureName;

    bool usesConnectedTextures = false;

    Dictionary<int, Rect> connectedTextures = new Dictionary<int, Rect>();

    List<Rect> textures = new List<Rect>();

    public TextureCollection(string name)
    {
        textureName = name;
    }

    public void AddTexture (Rect texture, int connectedTextureType) {
        if (connectedTextureType != -1)
        {
            //usesConnectedTextures = true;
            //connectedTextures.Add(connectedTextureType, texture);

            //if (connectedTextures[-2] != new Rect() && connectedTextures[-3] != new Rect())
            //{
            //    GenerateConnectedTextures();
            //}
        }
        else
        {
            textures.Add(texture);
        }
    }

    void GenerateConnectedTextures()
    {

    }

    public Rect GetTexture(Chunk chunk, BlockPos pos)
    {
        if (usesConnectedTextures)
        {
            return new Rect();
        }

        if (textures.Count == 1)
        {
            return textures[0];
        }

        if (textures.Count > 1)
        {
            float randomNumber = SimplexNoise.Noise.Generate(pos.x, pos.y, pos.z);
            randomNumber += 1;
            randomNumber /= 2;
            randomNumber *= textures.Count;

            return textures[(int)randomNumber];
        }


        Debug.LogError("There were no textures for " + textureName);
        return new Rect();
    }

}
