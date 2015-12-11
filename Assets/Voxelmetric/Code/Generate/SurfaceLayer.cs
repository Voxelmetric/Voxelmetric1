using System;
using SimplexNoise;
using UnityEngine;
public class SurfaceLayer : TerrainLayer
{
    // Right now this acts just like additive layer but always set to one block thickness
    // but it's a placeholder so that in the future we can do things like blend surface layers
    // between separate biomes

    Block blockToPlace;

    protected override void SetUp(LayerConfig config)
    {
        blockToPlace = new Block(properties["blockName"], world);

        if (properties.ContainsKey("blockColors"))
        {
            string[] colors = properties["blockColors"].Split(',');
            blockToPlace = BlockColored.SetBlockColor(blockToPlace, byte.Parse(colors[0]), byte.Parse(colors[1]), byte.Parse(colors[2]));
        }
    }

    public override int GenerateLayer(Chunk[] chunks, int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {

        //If we're not just getting the height apply the changes
        if (!justGetHeight)
        {
            SetBlocksColumn(chunks, x, z, heightSoFar, heightSoFar + 1, blockToPlace);
        }

        return heightSoFar + 1;
    }
}
