using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Load_Resources.Textures
{
    public static class ConnectedTextures {

        public static bool IsSame(Chunk chunk, Vector3Int globalsPos, int h, int v, Direction dir, int type)
        {
            return chunk.blocks.Get(RelativePos(globalsPos, h, v, dir)).Type == type;
        }

        public static Vector3Int RelativePos(Vector3Int pos, int h, int v, Direction dir)
        {
            switch (dir)
            {
                case Direction.up:
                    return pos.Add(v, 0, h);
                case Direction.down:
                    return pos.Add(v, 0, -h);
                case Direction.north:
                    return pos.Add(h, v, 0);
                case Direction.south:
                    return pos.Add(-h, v, 0);
                case Direction.east:
                    return pos.Add(0, v, -h);
                case Direction.west:
                    return pos.Add(0, v, h);
                default:
                    return pos;
            }
        }

        public static int GetTexture(bool n, bool e, bool s, bool w, bool wn, bool ne, bool es, bool sw)
        {
            if (n && !w && e && s && ne && es)
                return 27;

            if (n && !w && e && s && !sw && ne)
                return 31;

            if (n && w && !e && s && !wn && sw)
                return 44;

            if (n && w && e && s && wn && ne && es && sw)
                return 1;

            if (!n && !w && !e && !s)
                return 0;

            if (!n && w && e && !s)
                return 2;

            if (n && !w && !e && s)
                return 3;

            if (n && w && e && s && !wn && !ne && !es && !sw)
                return 4;

            if (!n && !w && e && !s)
                return 5;

            if (!n && w && !e && !s)
                return 6;

            if (!n && !w && e && s && !es)
                return 7;

            if (!n && w && !e && s && !sw)
                return 8;

            if (n && !w && e && s && !ne && !es)
                return 9;

            if (!n && w && e && s && !es && !sw)
                return 10;

            if (n && w && e && s && !wn && ne && !es && !sw)
                return 11;

            if (n && w && e && s && !wn && !ne && es && !sw)
                return 12;

            if (n && w && e && s && wn && !ne && !es && sw)
                return 13;

            if (n && w && e && s && wn && ne && !es && !sw)
                return 14;

            if (!n && !w && !e && s)
                return 15;

            if (!n && !w && e && s && es)
                return 16;

            if (!n && w && e && s && es && sw)
                return 17;

            if (!n && w && !e && s && sw)
                return 18;

            if (n && !w && e && !s && !ne)
                return 19;

            if (n && w && !e && !s && !wn)
                return 20;

            if (n && w && e && !s && !wn && !ne)
                return 21;

            if (n && w && !e && s && !wn && !sw)
                return 22;

            if (n && w && e && s && wn && !ne && !es && !sw)
                return 23;

            if (n && w && e && s && !wn && !ne && !es && sw)
                return 24;

            if (n && w && e && s && !wn && !ne && es && sw)
                return 25;

            if (n && w && e && s && !wn && ne && es && !sw)
                return 26;

            if (n && w && !e && s && wn && sw)
                return 28;

            if (n && !w && e && s && !ne && es)
                return 29;

            if (!n && w && e && s && !es && sw)
                return 30;

            if (n && !w && e && s && ne && !es)
                return 31;

            if (!n && w && e && s && es && !sw)
                return 32;

            if (n && w && e && s && wn && ne && !es && sw)
                return 33;

            if (n && w && e && s && wn && ne && es && !sw)
                return 34;

            if (n && w && e && s && !wn && ne && !es && sw)
                return 35;

            if (n && w && e && s && wn && !ne && es && !sw)
                return 36;

            if (n && !w && !e && !s)
                return 37;

            if (n && !w && e && !s && ne)
                return 38;

            if (n && w && e && !s && wn && ne)
                return 39;

            if (n && w && !e && !s && wn)
                return 40;

            if (n && w && e && !s && !wn && ne)
                return 41;

            if (n && w && e && !s && wn && !ne)
                return 43;

            if (n && w && !e && s && wn && !sw)
                return 42;

            if (n && w && e && s && wn && !ne && es && sw)
                return 45;

            if (n && w && e && s && !wn && ne && es && sw)
                return 46;

            return 0;
        }

        public static Texture2D[] ConnectedTexturesFromBaseTextures(TextureConfig.Texture[] baseTextures)
        {
            Texture2D[] textures = new Texture2D[48];
            for (int i = 0; i < baseTextures.Length; i++)
            {
                textures[baseTextures[i].connectedType] = baseTextures[i].texture2d;
            }

            int width = textures[0].width;
            int height = textures[0].height;

            //All of these are in order of wn, ne, es, sw
            List<Color[]> surrounded = GetTextureQuads(textures[0]);
            List<Color[]> full = GetTextureQuads(textures[1]);
            List<Color[]> horizontal = GetTextureQuads(textures[2]);
            List<Color[]> vertical = GetTextureQuads(textures[3]);
            List<Color[]> corners = GetTextureQuads(textures[4]);

            textures[5] = TextureFromColorQuads(surrounded[0], horizontal[1], horizontal[2], surrounded[3], width, height);
            textures[6] = TextureFromColorQuads(horizontal[0], surrounded[1], surrounded[2], horizontal[3], width, height);
            textures[7] = TextureFromColorQuads(surrounded[0], horizontal[1], corners[2], vertical[3], width, height);
            textures[8] = TextureFromColorQuads(horizontal[0], surrounded[1], vertical[2], corners[3], width, height);
            textures[9] = TextureFromColorQuads(vertical[0], corners[1], corners[2], vertical[3], width, height);
            textures[10] = TextureFromColorQuads(horizontal[0], horizontal[1], corners[2], corners[3], width, height);
            textures[11] = TextureFromColorQuads(corners[0], full[1], corners[2], corners[3], width, height);
            textures[12] = TextureFromColorQuads(corners[0], corners[1], full[2], corners[3], width, height);
            textures[13] = TextureFromColorQuads(full[0], corners[1], corners[2], full[3], width, height);
            textures[14] = TextureFromColorQuads(full[0], full[1], corners[2], corners[3], width, height);
            //line2
            textures[15] = TextureFromColorQuads(surrounded[0], surrounded[1], vertical[2], vertical[3], width, height);
            textures[16] = TextureFromColorQuads(surrounded[0], horizontal[1], full[2], vertical[3], width, height);
            textures[17] = TextureFromColorQuads(horizontal[0], horizontal[1], full[2], full[3], width, height);
            textures[18] = TextureFromColorQuads(horizontal[0], surrounded[1], vertical[2], full[3], width, height);
            textures[19] = TextureFromColorQuads(vertical[0], corners[1], horizontal[2], surrounded[3], width, height);
            textures[20] = TextureFromColorQuads(corners[0], vertical[1], surrounded[2], horizontal[3], width, height);
            textures[21] = TextureFromColorQuads(corners[0], corners[1], horizontal[2], horizontal[3], width, height);
            textures[22] = TextureFromColorQuads(corners[0], vertical[1], vertical[2], corners[3], width, height);
            textures[23] = TextureFromColorQuads(full[0], corners[1], corners[2], corners[3], width, height);
            textures[24] = TextureFromColorQuads(corners[0], corners[1], corners[2], full[3], width, height);
            textures[25] = TextureFromColorQuads(corners[0], corners[1], full[2], full[3], width, height);
            textures[26] = TextureFromColorQuads(corners[0], full[1], full[2], corners[3], width, height);
            //line3
            textures[27] = TextureFromColorQuads(vertical[0], full[1], full[2], vertical[3], width, height);
            textures[28] = TextureFromColorQuads(full[0], vertical[1], vertical[2], full[3], width, height);
            textures[29] = TextureFromColorQuads(vertical[0], corners[1], full[2], vertical[3], width, height);
            textures[30] = TextureFromColorQuads(horizontal[0], horizontal[1], corners[2], full[3], width, height);
            textures[31] = TextureFromColorQuads(vertical[0], full[1], corners[2], vertical[3], width, height);
            textures[32] = TextureFromColorQuads(horizontal[0], horizontal[1], full[2], corners[3], width, height);
            textures[33] = TextureFromColorQuads(full[0], full[1], corners[2], full[3], width, height);
            textures[34] = TextureFromColorQuads(full[0], full[1], full[2], corners[3], width, height);
            textures[35] = TextureFromColorQuads(corners[0], full[1], corners[2], full[3], width, height);
            textures[36] = TextureFromColorQuads(full[0], corners[1], full[2], corners[3], width, height);
            //line4
            textures[37] = TextureFromColorQuads(vertical[0], vertical[1], surrounded[2], surrounded[3], width, height);
            textures[38] = TextureFromColorQuads(vertical[0], full[1], horizontal[2], surrounded[3], width, height);
            textures[39] = TextureFromColorQuads(full[0], full[1], horizontal[2], horizontal[3], width, height);
            textures[40] = TextureFromColorQuads(full[0], vertical[1], surrounded[2], horizontal[3], width, height);
            textures[41] = TextureFromColorQuads(corners[0], full[1], horizontal[2], horizontal[3], width, height);
            textures[42] = TextureFromColorQuads(full[0], vertical[1], vertical[2], corners[3], width, height);
            textures[43] = TextureFromColorQuads(full[0], corners[1], horizontal[2], horizontal[3], width, height);
            textures[44] = TextureFromColorQuads(corners[0], vertical[1], vertical[2], full[3], width, height);
            textures[45] = TextureFromColorQuads(full[0], corners[1], full[2], full[3], width, height);
            textures[46] = TextureFromColorQuads(corners[0], full[1], full[2], full[3], width, height);
            return textures;
        }

        public static List<Color[]> GetTextureQuads(Texture2D texture)
        {
            List<Color[]> textures = new List<Color[]>();
            int halfWidth = texture.width / 2;
            int halfHeight = texture.height / 2;

            textures.Add(texture.GetPixels(0, halfHeight, halfWidth, halfHeight));
            textures.Add(texture.GetPixels(halfWidth, halfHeight, halfWidth, halfHeight));
            textures.Add(texture.GetPixels(halfWidth, 0, halfWidth, halfHeight));
            textures.Add(texture.GetPixels(0, 0, halfWidth, halfHeight));

            return textures;
        }

        public static Texture2D TextureFromColorQuads(Color[] wn, Color[] ne, Color[] es, Color[] sw, int width, int height)
        {
            Texture2D texture = new Texture2D(width, height, TextureFormat.ARGB32, false);
            int halfWidth = texture.width / 2;
            int halfHeight = texture.height / 2;

            texture.SetPixels(0, halfHeight, halfWidth, halfHeight, wn);
            texture.SetPixels(halfWidth, halfHeight, halfWidth, halfHeight, ne);
            texture.SetPixels(halfWidth, 0, halfWidth, halfHeight, es);
            texture.SetPixels(0, 0, halfWidth, halfHeight, sw);

            return texture;
        }
    }
}
