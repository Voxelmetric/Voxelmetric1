using System;
using SimplexNoise;
using UnityEngine;
public class RandomLayer: TerrainLayer
{
    Block blockToPlace;
    float chance;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for random layers MUST define these properties
        blockToPlace = new Block(properties["blockName"], world);
        chance = float.Parse(properties["chance"]);
    }

    public override int GenerateLayer(Chunk[] chunks, int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        float posChance = new BlockPos(x, heightSoFar + 1, z).Random(200);

        if (chance > posChance)
        {
            if (!justGetHeight)
                SetBlocksColumn(chunks, x, z, heightSoFar, heightSoFar + 1, blockToPlace);

            return heightSoFar + 1;
        }
        else
        {
            return heightSoFar;
        }
    }
}
