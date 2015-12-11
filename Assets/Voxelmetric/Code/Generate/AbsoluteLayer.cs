using System;
using SimplexNoise;
using UnityEngine;

public class AbsoluteLayer : TerrainLayer
{
    Block blockToPlace;
    float frequency;
    float exponent;
    int minHeight;
    int maxHeight;
    int amplitude;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for absolute layers MUST define these properties
        blockToPlace = new Block(properties["blockName"], world);

        if (properties.ContainsKey("blockColors"))
        {
            string[] colors = properties["blockColors"].Split(',');
            blockToPlace = BlockColored.SetBlockColor(blockToPlace, byte.Parse(colors[0]), byte.Parse(colors[1]), byte.Parse(colors[2]));
        }

        frequency = float.Parse(properties["frequency"]);
        exponent = float.Parse(properties["exponent"]);
        minHeight = int.Parse(properties["minHeight"]);
        maxHeight = int.Parse(properties["maxHeight"]);

        amplitude = maxHeight - minHeight;
    }

    public override int GenerateLayer(Chunk[] chunks, int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        // Calculate height to add with the perlin noise using settings from the config
        // and add to that the min height from the config (because the height of this
        // layer should fluctuate between that min height and the min height + the max noise
        // And multiply by strength so that it can decide the fraction of the result that gets used
        int heightToAdd = GetNoise(x, 0, z, frequency, amplitude, exponent);
        heightToAdd += minHeight;
        heightToAdd = (int)(heightToAdd * strength);

        //Absolute layers add from the minY and up but if the layer height is
        // lower than the existing terrain there's nothing to add so just return the initial value
        if (world.config.minY + heightToAdd > heightSoFar)
        {
            //If we're not just getting the height apply the changes
            if (!justGetHeight)
            {
                SetBlocksColumn(chunks, x, z, heightSoFar, world.config.minY + heightToAdd, blockToPlace);
            }

            //Return the height of this layer from minY as this is the new height of the column
            return world.config.minY + heightToAdd;
        }
        else
        {
            return heightSoFar;
        }
    }
}
