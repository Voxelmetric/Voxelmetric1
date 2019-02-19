using UnityEngine;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public static class ConnectedTextures
    {
        public static int GetTexture(bool n, bool e, bool s, bool w, bool nw, bool ne, bool se, bool sw)
        {
            if (!n && !w && !e && !s)
                return 0;

            if (n && w && e && s && nw && ne && se && sw)
                return 1;

            if (!n && w && e && !s)
                return 2;

            if (n && !w && !e && s)
                return 3;

            if (n && w && e && s && !nw && !ne && !se && !sw)
                return 4;

            if (!n && !w && e && !s)
                return 5;

            if (!n && w && !e && !s)
                return 6;

            if (!n && !w && e && s && !se)
                return 7;

            if (!n && w && !e && s && !sw)
                return 8;

            if (n && !w && e && s && !ne && !se)
                return 9;

            if (!n && w && e && s && !se && !sw)
                return 10;

            if (n && w && e && s && !nw && ne && !se && !sw)
                return 11;

            if (n && w && e && s && !nw && !ne && se && !sw)
                return 12;

            if (n && w && e && s && nw && !ne && !se && sw)
                return 13;

            if (n && w && e && s && nw && ne && !se && !sw)
                return 14;

            if (!n && !w && !e && s)
                return 15;

            if (!n && !w && e && s && se)
                return 16;

            if (!n && w && e && s && se && sw)
                return 17;

            if (!n && w && !e && s && sw)
                return 18;

            if (n && !w && e && !s && !ne)
                return 19;

            if (n && w && !e && !s && !nw)
                return 20;

            if (n && w && e && !s && !nw && !ne)
                return 21;

            if (n && w && !e && s && !nw && !sw)
                return 22;

            if (n && w && e && s && nw && !ne && !se && !sw)
                return 23;

            if (n && w && e && s && !nw && !ne && !se && sw)
                return 24;

            if (n && w && e && s && !nw && !ne && se && sw)
                return 25;

            if (n && w && e && s && !nw && ne && se && !sw)
                return 26;

            if (n && !w && e && s && ne && se)
                return 27;

            if (n && w && !e && s && nw && sw)
                return 28;

            if (n && !w && e && s && !ne && se)
                return 29;

            if (!n && w && e && s && !se && sw)
                return 30;
            
            if (n && !w && e && s && ne && !se)
                return 31;

            if (!n && w && e && s && se && !sw)
                return 32;

            if (n && w && e && s && nw && ne && !se && sw)
                return 33;

            if (n && w && e && s && nw && ne && se && !sw)
                return 34;

            if (n && w && e && s && !nw && ne && !se && sw)
                return 35;

            if (n && w && e && s && nw && !ne && se && !sw)
                return 36;

            if (n && !w && !e && !s)
                return 37;

            if (n && !w && e && !s && ne)
                return 38;

            if (n && w && e && !s && nw && ne)
                return 39;

            if (n && w && !e && !s && nw)
                return 40;

            if (n && w && e && !s && !nw && ne)
                return 41;
            
            if (n && w && !e && s && nw && !sw)
                return 42;

            if (n && w && e && !s && nw && !ne)
                return 43;

            if (n && w && !e && s && !nw && sw)
                return 44;

            if (n && w && e && s && nw && !ne && se && sw)
                return 45;

            if (n && w && e && s && !nw && ne && se && sw)
                return 46;

            return 0;
        }

        public static Texture2D[] ConnectedTexturesFromBaseTextures(TextureConfig.Texture[] baseTextures)
        {
            Texture2D[] textures = new Texture2D[48];

            // Line 0
            for (int i = 0; i < baseTextures.Length; i++)
                textures[i] = baseTextures[i].texture2d;

            int w = textures[0].width;
            int h = textures[0].height;

            // All of these are in order of sw, nw, ne, se:
            // 1----2
            // |    |
            // |    |
            // 0----3
            Color[][] surrounded = GetTextureQuads(textures[0]);
            Color[][] full = GetTextureQuads(textures[1]);
            Color[][] horizontal = GetTextureQuads(textures[2]);
            Color[][] vertical = GetTextureQuads(textures[3]);
            Color[][] corners = GetTextureQuads(textures[4]);

            // Line 1
            textures[5] = TextureFromColorQuads(surrounded[3], surrounded[0], horizontal[1], horizontal[2], w, h);
            textures[6] = TextureFromColorQuads(horizontal[3], horizontal[0], surrounded[1], surrounded[2], w, h);
            textures[7] = TextureFromColorQuads(vertical[3], surrounded[0], horizontal[1], corners[2], w, h);
            textures[8] = TextureFromColorQuads(corners[3], horizontal[0], surrounded[1], vertical[2], w, h);
            textures[9] = TextureFromColorQuads(vertical[3], vertical[0], corners[1], corners[2], w, h);
            textures[10] = TextureFromColorQuads(corners[3], horizontal[0], horizontal[1], corners[2], w, h);
            textures[11] = TextureFromColorQuads(corners[3], corners[0], full[1], corners[2], w, h);
            textures[12] = TextureFromColorQuads(corners[3], corners[0], corners[1], full[2], w, h);
            textures[13] = TextureFromColorQuads(full[3], full[0], corners[1], corners[2], w, h);
            textures[14] = TextureFromColorQuads(corners[3], full[0], full[1], corners[2], w, h);
            // Line 2
            textures[15] = TextureFromColorQuads(vertical[3], surrounded[0], surrounded[1], vertical[2], w, h);
            textures[16] = TextureFromColorQuads(vertical[3], surrounded[0], horizontal[1], full[2], w, h);
            textures[17] = TextureFromColorQuads(full[3], horizontal[0], horizontal[1], full[2], w, h);
            textures[18] = TextureFromColorQuads(full[3], horizontal[0], surrounded[1], vertical[2], w, h);
            textures[19] = TextureFromColorQuads(surrounded[3], vertical[0], corners[1], horizontal[2], w, h);
            textures[20] = TextureFromColorQuads(horizontal[3], corners[0], vertical[1], surrounded[2], w, h);
            textures[21] = TextureFromColorQuads(horizontal[3], corners[0], corners[1], horizontal[2], w, h);
            textures[22] = TextureFromColorQuads(corners[3], corners[0], vertical[1], vertical[2], w, h);
            textures[23] = TextureFromColorQuads(corners[3], full[0], corners[1], corners[2], w, h);
            textures[24] = TextureFromColorQuads(full[3], corners[0], corners[1], corners[2], w, h);
            textures[25] = TextureFromColorQuads(full[3], corners[0], corners[1], full[2], w, h);
            textures[26] = TextureFromColorQuads(corners[3], corners[0], full[1], full[2], w, h);
            // Line 3
            textures[27] = TextureFromColorQuads(vertical[3], vertical[0], full[1], full[2], w, h);
            textures[28] = TextureFromColorQuads(full[3], full[0], vertical[1], vertical[2], w, h);
            textures[29] = TextureFromColorQuads(vertical[3], vertical[0], corners[1], full[2], w, h);
            textures[30] = TextureFromColorQuads(full[3], horizontal[0], horizontal[1], corners[2], w, h);
            textures[31] = TextureFromColorQuads(vertical[3], vertical[0], full[1], corners[2], w, h);
            textures[32] = TextureFromColorQuads(corners[3], horizontal[0], horizontal[1], full[2], w, h);
            textures[33] = TextureFromColorQuads(full[3], full[0], full[1], corners[2], w, h);
            textures[34] = TextureFromColorQuads(corners[3], full[0], full[1], full[2], w, h);
            textures[35] = TextureFromColorQuads(full[3], corners[0], full[1], corners[2], w, h);
            textures[36] = TextureFromColorQuads(corners[3], full[0], corners[1], full[2], w, h);
            // Line 4
            textures[37] = TextureFromColorQuads(surrounded[3], vertical[0], vertical[1], surrounded[2], w, h);
            textures[38] = TextureFromColorQuads(surrounded[3], vertical[0], full[1], horizontal[2], w, h);
            textures[39] = TextureFromColorQuads(horizontal[3], full[0], full[1], horizontal[2], w, h);
            textures[40] = TextureFromColorQuads(horizontal[3], full[0], vertical[1], surrounded[2], w, h);
            textures[41] = TextureFromColorQuads(horizontal[3], corners[0], full[1], horizontal[2], w, h);
            textures[42] = TextureFromColorQuads(corners[3], full[0], vertical[1], vertical[2], w, h);
            textures[43] = TextureFromColorQuads(horizontal[3], full[0], corners[1], horizontal[2], w, h);
            textures[44] = TextureFromColorQuads(full[3], corners[0], vertical[1], vertical[2], w, h);
            textures[45] = TextureFromColorQuads(full[3], full[0], corners[1], full[2], w, h);
            textures[46] = TextureFromColorQuads(full[3], corners[0], full[1], full[2], w, h);

            return textures;
        }

        public static Color[][] GetTextureQuads(Texture2D texture)
        {
            Color[][] textures = new Color[4][];
            int halfWidth = texture.width >> 1;
            int halfHeight = texture.height >> 1;

            textures[0] = texture.GetPixels(0, 0, halfWidth, halfHeight);
            textures[1] = texture.GetPixels(0, halfHeight, halfWidth, halfHeight);
            textures[2] = texture.GetPixels(halfWidth, halfHeight, halfWidth, halfHeight);
            textures[3] = texture.GetPixels(halfWidth, 0, halfWidth, halfHeight);

            return textures;
        }

        public static Texture2D TextureFromColorQuads(Color[] sw, Color[] nw, Color[] ne, Color[] se, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            int halfWidth = texture.width >> 1;
            int halfHeight = texture.height >> 1;

            texture.SetPixels(0, 0, halfWidth, halfHeight, sw);
            texture.SetPixels(0, halfHeight, halfWidth, halfHeight, nw);
            texture.SetPixels(halfWidth, halfHeight, halfWidth, halfHeight, ne);
            texture.SetPixels(halfWidth, 0, halfWidth, halfHeight, se);

            return texture;
        }
    }
}
