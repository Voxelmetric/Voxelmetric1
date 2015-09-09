using UnityEngine;
using System.Collections.Generic;

public static class uPaddingBleed
{
    struct Tile
    {
        public int minX, maxX, minY, maxY, width, height;
    }

    public static void BleedEdges(Texture2D texture, int padding, Rect[] texturePositions, bool repeatingTextures)
    {
        if (padding == 0)
            return;

        padding /= 2;

        Tile tile = new Tile();
        for (int i = 0; i < texturePositions.Length; i++)
        {
            //Storing all the information we need about the rect in one struct to make them easier to access
            tile.minX = (int)(texture.width * texturePositions[i].min.x);
            tile.maxX = (int)(texture.width * texturePositions[i].max.x);
            tile.minY = (int)(texture.height * texturePositions[i].min.y);
            tile.maxY = (int)(texture.height * texturePositions[i].max.y);
            tile.width = (int)(texture.width * texturePositions[i].width);
            tile.height = (int)(texture.height * texturePositions[i].height);

            //There are 8 sections in the padding that need to be assigned

            /* 

             paddingWN  |  paddingN  |  paddingNE
             ___________|____________|___________
                        |            |
                        |            |
              paddingW  |    tile    |  paddingE
                        |            |
             ___________|____________|___________
                        |            |   
             paddingSW  |  paddingS  |  paddingES

            */

            Color[] paddingWN, paddingN, paddingNE, paddingW, paddingE, paddingSW, paddingS, paddingES;
            // Repeat the texture in the padding
            // With repeating textures it isn't enough to stretch edge pixels,
            // the edges wont match up when we use bilinear or trilinear filtering
            if (repeatingTextures)
            {
                paddingWN = texture.GetPixels(OffsetPos(tile.maxX, -padding, texture.width), tile.minY, padding, padding);
                paddingN  = texture.GetPixels(tile.minX, tile.minY, tile.width, padding);
                paddingNE = texture.GetPixels(tile.minX, tile.minY, padding, padding);

                paddingW  = texture.GetPixels(OffsetPos(tile.maxX, -padding, texture.width), tile.minY, padding, tile.height);
                paddingE  = texture.GetPixels(tile.minX, tile.minY, padding, tile.height);

                paddingSW = texture.GetPixels(OffsetPos(tile.maxX, -padding, texture.width), OffsetPos(tile.maxY, -padding, texture.height), padding, padding);
                paddingS  = texture.GetPixels(tile.minX, OffsetPos(tile.maxY, -padding, texture.height), tile.width, padding);
                paddingES = texture.GetPixels(tile.minX, OffsetPos(tile.maxY, -padding, texture.height), padding, padding);
            }
            else
            {
                paddingN = texture.GetPixels(tile.minX, tile.maxY - 1, tile.width, 1);
                if (paddingN.Length == 0)
                    continue;

                paddingN = StretchPaddingH(paddingN, padding, tile);

                paddingS = texture.GetPixels(tile.minX, tile.minY, tile.width, 1);
                if (paddingS.Length == 0)
                    continue;

                paddingS = StretchPaddingH(paddingS, padding, tile);

                paddingE = texture.GetPixels(tile.maxX - 1, tile.minY, 1, tile.height);
                if (paddingE.Length == 0)
                    continue;

                paddingE = StretchPaddingV(paddingE, padding, tile);

                paddingW = texture.GetPixels(tile.minX, tile.minY, 1, tile.height);

                if (paddingW.Length == 0)
                    continue;

                paddingW = StretchPaddingV(paddingW, padding, tile);

                paddingWN = new Color[padding * padding];
                paddingNE = new Color[padding * padding];

                paddingSW = new Color[padding * padding];
                paddingES = new Color[padding * padding];

                for (int n = 0; n < padding * padding; n++)
                {
                    paddingWN[n] = paddingN[0];
                    paddingNE[n] = paddingN[paddingN.Length-1];
                    paddingSW[n] = paddingS[0];
                    paddingES[n] = paddingS[paddingN.Length - 1];
                }
            }

            texture.SetPixels(OffsetPos(tile.minX, -padding, texture.width), tile.maxY, padding, padding, paddingWN);
            texture.SetPixels(tile.minX, tile.maxY, tile.width, padding, paddingN);
            texture.SetPixels(tile.maxX, tile.maxY, padding, padding, paddingNE);

            texture.SetPixels(OffsetPos(tile.minX, -padding, texture.width), tile.minY, padding, tile.height, paddingW);
            texture.SetPixels(tile.maxX, tile.minY, padding, tile.height, paddingE);

            texture.SetPixels(OffsetPos(tile.minX, -padding, texture.width), OffsetPos(tile.minY, -padding, texture.height), padding, padding, paddingSW);
            texture.SetPixels(tile.minX, OffsetPos(tile.minY, -padding, texture.height), tile.width, padding, paddingS);
            texture.SetPixels(tile.maxX, OffsetPos(tile.minY, -padding, texture.height), padding, padding, paddingES);
        }

        texture.Apply();
    }

    static int OffsetPos(int pos, int offset, int max)
    {
        int value = pos + offset;
        if (value > max)
        {
            value = value - max;
        }
        else if (value < 0)
        {
            value = max + value;
        }

        return value;
    }

    static Color[] StretchPaddingH(Color[] pixels, int padding, Tile tile)
    {
        Color[] paddingArray = new Color[pixels.Length * padding];

        for (int n = 0; n < padding; n++)
            for (int j = 0; j < pixels.Length; j++)
                paddingArray[j + (n * pixels.Length)] = pixels[j];

        return paddingArray;
    }

    static Color[] StretchPaddingV(Color[] pixels, int padding, Tile tile)
    {
        Color[] paddingArray = new Color[pixels.Length * padding];

        for (int j = 0; j < pixels.Length; j++)
            for (int n = 0; n < padding; n++)
                paddingArray[n +(j*padding)] = pixels[j];

        return paddingArray;
    }
}
