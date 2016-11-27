using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public class TextureCollection
    {
        public string textureName;
        bool usesConnectedTextures = false;
        Rect[] connectedTextures = new Rect[48];
        List<Rect> textures = new List<Rect>();

        public TextureCollection(string name)
        {
            textureName = name;
        }

        public void AddTexture(Rect texture, int connectedTextureType, int randomChance)
        {
            if (connectedTextureType != -1)
            {
                usesConnectedTextures = true;
                connectedTextures[connectedTextureType] = texture;
            }
            else if (randomChance > 1)
            {
                // Add the texture multiple times to raise the chance it's selected randomly
                for (int i = 0; i < randomChance; i++)
                {
                    textures.Add(texture);
                }
            }
            else
            {
                textures.Add(texture);
            }
        }

        public Rect GetTexture(Chunk chunk, Vector3Int localPos, Direction direction)
        {
            if (usesConnectedTextures)
            {
                int blockType = chunk.blocks.Get(localPos).Type;

                bool wn = ConnectedTextures.IsSame(chunk, localPos, -1, 1, direction, blockType);
                bool n = ConnectedTextures.IsSame(chunk, localPos, 0, 1, direction, blockType);
                bool ne = ConnectedTextures.IsSame(chunk, localPos, 1, 1, direction, blockType);
                bool w = ConnectedTextures.IsSame(chunk, localPos, -1, 0, direction, blockType);
                bool e = ConnectedTextures.IsSame(chunk, localPos, 1, 0, direction, blockType);
                bool es = ConnectedTextures.IsSame(chunk, localPos, 1, -1, direction, blockType);
                bool s = ConnectedTextures.IsSame(chunk, localPos, 0, -1, direction, blockType);
                bool sw = ConnectedTextures.IsSame(chunk, localPos, -1, -1, direction, blockType);

                return connectedTextures[ConnectedTextures.GetTexture(n, e, s, w, wn, ne, es, sw)];
            }

            if (textures.Count == 1)
            {
                return textures[0];
            }

            if (textures.Count > 1)
            {
                int hash = localPos.GetHashCode();
                if (hash < 0)
                    hash *= -1;

                float randomNumber = (hash % 100) /100f;
                randomNumber *= textures.Count;

                return textures[(int)randomNumber];
            }


            Debug.LogError("There were no textures for " + textureName);
            return new Rect();
        }



    }
}
