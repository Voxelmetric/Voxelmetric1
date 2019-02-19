using UnityEngine;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public enum TextureConfigType
    {
        Simple = 0,
        Connected
    }

    public struct TextureConfig {

        public string name;
        public TextureConfigType type;

        public Texture[] textures;

        public struct Texture
        {
            public string file;
            public Texture2D texture2d;

            public int weight;
            public int index;

            public int x;
            public int y;
            public int width;
            public int height;
            public bool repeating;
        }

        public override string ToString()
        {
            return name;
        }
    }
}
