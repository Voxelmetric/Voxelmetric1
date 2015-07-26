using UnityEngine;
using System.Collections;

public class CavesLayer : LayerOverride {

    public override int GenerateLayer(int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {

        Block blockToPlace = blockName; 
        blockToPlace.modified = false;

        int caveBottom = GetNoise(x, -1000, z, 500, 70, 1) + Config.Env.WorldMinY;
        int caveHeight = GetNoise(x, 1000, z, 50, 35, 1) + caveBottom;

        caveHeight -= 20;

        if (caveHeight > caveBottom)
        {
            caveBottom -= caveHeight / 2;
            int caveTop = caveHeight / 2;
            if (!justGetHeight)
            {
                for (int y = caveBottom; y < caveTop; y++)
                {
                    world.SetBlock(new BlockPos(x, y, z), blockToPlace, false);
                }
            }

            if (caveTop > heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }
}
