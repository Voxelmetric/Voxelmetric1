using UnityEngine;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public struct TextureConfig {

        public string name;

        public bool connectedTextures;
        public bool randomTextures;

        public Texture[] textures;

        public struct Texture
        {
            public string file;
            public Texture2D texture2d;
            public int connectedType;
            public int weight;

            public int xPos;
            public int yPos;
            public int width;
            public int height;

            public bool repeatingTexture;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
