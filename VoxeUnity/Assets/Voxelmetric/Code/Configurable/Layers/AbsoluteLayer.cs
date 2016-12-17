using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;
using Voxelmetric.Code.Utilities.Noise;

public class AbsoluteLayer : TerrainLayer
{
    private Block blockToPlace;
    private float frequency;
    private float exponent;
    private int minHeight;
    private int maxHeight;
    private int amplitude;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for absolute layers MUST define these properties
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

    public override void PreProcess(Chunk chunk, int layerIndex)
    {
        NoiseItem ni = chunk.pools.noiseItems[layerIndex];
        ni.noiseGen.SetInterpBitStep(Env.ChunkSize, 2);
        ni.lookupTable = chunk.pools.PopFloatArray(ni.noiseGen.Size*ni.noiseGen.Size);

        int xOffset = chunk.pos.x;
        int zOffset = chunk.pos.z;

        // Generate a lookup table
        int i = 0;
        for (int z = 0; z<ni.noiseGen.Size; z++)
        {
            float zf = (z<<ni.noiseGen.Step)+zOffset;

            for (int x = 0; x<ni.noiseGen.Size; x++)
            {
                float xf = (x<<ni.noiseGen.Step)+xOffset;
                ni.lookupTable[i++] = GetNoise(xf, 0, zf, frequency, amplitude, exponent);
            }
        }
    }

    public override void PostProcess(Chunk chunk, int layerIndex)
    {
        NoiseItem ni = chunk.pools.noiseItems[layerIndex];
        chunk.pools.PushFloatArray(ni.lookupTable);
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        NoiseItem ni = chunk.pools.noiseItems[layerIndex];

        // Calculate height to add and sum it with the min height (because the height of this
        // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
        // it by strength so that a fraction of the result that gets used can be decided
        float heightToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
        heightToAdd += minHeight;
        heightToAdd = heightToAdd*strength;

        // Absolute layers add from the minY and up but if the layer height is lower than
        // the existing terrain there's nothing to add so just return the initial value
        if (heightToAdd > heightSoFar)
        {
            //Return the height of this layer from minY as this is the new height of the column
            return heightToAdd;
        }

        return heightSoFar;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        NoiseItem ni = chunk.pools.noiseItems[layerIndex];

        // Calculate height to add and sum it with the min height (because the height of this
        // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
        // it by strength so that a fraction of the result that gets used can be decided
        float heightToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
        heightToAdd += minHeight;
        heightToAdd = heightToAdd*strength;

        // Absolute layers add from the minY and up but if the layer height is lower than
        // the existing terrain there's nothing to add so just return the initial value
        if (heightToAdd > heightSoFar)
        {
            SetBlocks(chunk, x, z, (int)heightSoFar, (int)heightToAdd, blockToPlace);

            //Return the height of this layer from minY as this is the new height of the column
            return heightToAdd;
        }

        return heightSoFar;
    }
}
