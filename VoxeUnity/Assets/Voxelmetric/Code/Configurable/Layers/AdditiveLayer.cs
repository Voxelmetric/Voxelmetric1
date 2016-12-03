using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;

public class AdditiveLayer: TerrainLayer
{
    Block blockToPlace;
    float frequency;
    float exponent;
    int minHeight;
    int maxHeight;
    int amplitude;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for additive layers MUST define these properties
        blockToPlace = world.blockProvider.GetBlock(properties["blockName"]);

        if (properties.ContainsKey("blockColors"))
        {
            string[] colors = properties["blockColors"].Split(',');
            ((ColoredBlock)blockToPlace).color = new Color(byte.Parse(colors[0]) / 255f, byte.Parse(colors[1]) / 255f, byte.Parse(colors[2]) / 255f);
        }

        frequency = float.Parse(properties["frequency"]);
        exponent = float.Parse(properties["exponent"]);
        minHeight = int.Parse(properties["minHeight"]);
        maxHeight = int.Parse(properties["maxHeight"]);

        amplitude = maxHeight - minHeight;
    }

    public override int GetHeight(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        // Calculate height to add and sum it with the min height (because the height of this
        // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
        // it by strength so that a fraction of the result that gets used can be decided
        int heightToAdd = GetNoise(x, 0, z, frequency, amplitude, exponent);
        heightToAdd += minHeight;
        heightToAdd = (int)(heightToAdd * strength);

        return heightSoFar + heightToAdd;
    }

    public override int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        // Calculate height to add and sum it with the min height (because the height of this
        // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
        // it by strength so that a fraction of the result that gets used can be decided
        int heightToAdd = GetNoise(x, 0, z, frequency, amplitude, exponent);
        heightToAdd += minHeight;
        heightToAdd = (int)(heightToAdd * strength);

        SetBlocks(chunk, x, z, heightSoFar, heightSoFar + heightToAdd, blockToPlace);

        return heightSoFar + heightToAdd;
    }
}
