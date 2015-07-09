using UnityEngine;
using System.Collections.Generic;
using SimplexNoise;

public class TextureCollection
{

    public string textureName;
    bool usesConnectedTextures = false;
    Rect[] connectedTextures = new Rect[48];
    List<Rect> textures = new List<Rect>();
    Noise noiseGen;

    public TextureCollection(string name)
    {
        textureName = name;
        noiseGen = new Noise();
    }

    public void AddTexture(Rect texture, int connectedTextureType)
    {
        if (connectedTextureType != -1)
        {
            usesConnectedTextures = true;
            connectedTextures[connectedTextureType] = texture;
        }
        else
        {
            textures.Add(texture);
        }
    }

    public Rect GetTexture(Chunk chunk, BlockPos pos, Direction direction)
    {
        if (usesConnectedTextures)
        {
            string blockName = chunk.GetBlock(pos).controller.Name();

            bool wn = ConnectedTextures.IsSame(chunk, pos, -1, 1, direction, blockName);
            bool n = ConnectedTextures.IsSame(chunk, pos, 0, 1, direction, blockName);
            bool ne = ConnectedTextures.IsSame(chunk, pos, 1, 1, direction, blockName);
            bool w = ConnectedTextures.IsSame(chunk, pos, -1, 0, direction, blockName);
            bool e = ConnectedTextures.IsSame(chunk, pos, 1, 0, direction, blockName);
            bool es = ConnectedTextures.IsSame(chunk, pos, 1, -1, direction, blockName);
            bool s = ConnectedTextures.IsSame(chunk, pos, 0, -1, direction, blockName);
            bool sw = ConnectedTextures.IsSame(chunk, pos, -1, -1, direction, blockName);

            return connectedTextures[ConnectedTextures.GetTexture(n, e, s, w, wn, ne, es, sw)];

        }

        if (textures.Count == 1)
        {
            return textures[0];
        }

        if (textures.Count > 1)
        {
            float randomNumber = noiseGen.Generate(pos.x, pos.y, pos.z);
            randomNumber += 1;
            randomNumber /= 2;
            randomNumber *= textures.Count;

            return textures[(int)randomNumber];
        }


        Debug.LogError("There were no textures for " + textureName);
        return new Rect();
    }



}
