using System.Globalization;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities.Noise;

public class AdditiveLayer: TerrainLayer
{
    private BlockData blockToPlace;
    private int minHeight;
    private int maxHeight;
    private int amplitude;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for additive layers MUST define these properties
        Block block = world.blockProvider.GetBlock(properties["blockName"]);
        blockToPlace = new BlockData(block.Type, block.Solid);
        
        noise.Frequency = 1f/float.Parse(properties["frequency"], CultureInfo.InvariantCulture); // Frequency in configs is in fast 1/frequency
        noise.Gain = float.Parse(properties["exponent"], CultureInfo.InvariantCulture);
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
        noiseSIMD.Frequency = noise.Frequency;
        noiseSIMD.Gain = noise.Gain;
#endif
        minHeight = int.Parse(properties["minHeight"], CultureInfo.InvariantCulture);
        maxHeight = int.Parse(properties["maxHeight"], CultureInfo.InvariantCulture);

        amplitude = maxHeight - minHeight;
    }

    public override void PreProcess(Chunk chunk, int layerIndex)
    {
        var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        var ni = pools.noiseItems[layerIndex];
        ni.noiseGen.SetInterpBitStep(Env.ChunkSizeWithPadding, 2);
        ni.lookupTable = pools.FloatArrayPool.Pop((ni.noiseGen.Size+1)*(ni.noiseGen.Size+1));

#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
        float[] noiseSet = chunk.pools.FloatArrayPool.Pop(ni.noiseGen.Size * ni.noiseGen.Size * ni.noiseGen.Size);

        // Generate SIMD noise
        int offsetShift = Env.ChunkPow - ni.noiseGen.Step;
        int xStart = (chunk.pos.x * Env.ChunkSize) << offsetShift;
        int yStart = (chunk.pos.y * Env.ChunkSize) << offsetShift;
        int zStart = (chunk.pos.z * Env.ChunkSize) << offsetShift;
        float scaleModifier = 1 << ni.noiseGen.Step;
        noiseSIMD.Noise.FillNoiseSet(noiseSet, xStart, yStart, zStart, ni.noiseGen.Size, ni.noiseGen.Size, ni.noiseGen.Size, scaleModifier);

        // Generate a lookup table
        int i = 0;
        for (int z = 0; z < ni.noiseGen.Size; z++)
            for (int x = 0; x < ni.noiseGen.Size; x++)
                ni.lookupTable[i++] = NoiseUtilsSIMD.GetNoise(noiseSet, ni.noiseGen.Size, x, 0, z, amplitude, noise.Gain);

        pools.FloatArrayPool.Push(noiseSet);
#else
        int xOffset = chunk.Pos.x;
        int zOffset = chunk.Pos.z;

        // Generate a lookup table
        int i = 0;
        for (int z = 0; z < ni.noiseGen.Size; z++)
        {
            float zf = (z << ni.noiseGen.Step) + zOffset;

            for (int x = 0; x < ni.noiseGen.Size; x++)
            {
                float xf = (x << ni.noiseGen.Step) + xOffset;
                ni.lookupTable[i++] = NoiseUtils.GetNoise(noise.Noise, xf, 0, zf, 1f, amplitude, noise.Gain);
            }
        }
#endif
    }

    public override void PostProcess(Chunk chunk, int layerIndex)
    {
        var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        var ni = pools.noiseItems[layerIndex];
        pools.FloatArrayPool.Push(ni.lookupTable);
    }

    public override float GetTemperature(Chunk chunk, int layerIndex, int x, int z, float tempSoFar)
    {
        return tempSoFar;
    }

    public override float GetHumidity(Chunk chunk, int layerIndex, int x, int z, float humSoFar)
    {
        return humSoFar;
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float tempSoFar, float humSoFar, float strength)
    {
        var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        var ni = pools.noiseItems[layerIndex];

        // Calculate height to add and sum it with the min height (because the height of this
        // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
        // it by strength so that a fraction of the result that gets used can be decided
        float heightToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
        heightToAdd += minHeight;
        heightToAdd = heightToAdd * strength;

        return heightSoFar + heightToAdd;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float tempSoFar, float humSoFar, float strength)
    {
        var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
        NoiseItem ni = pools.noiseItems[layerIndex];

        // Calculate height to add and sum it with the min height (because the height of this
        // layer should fluctuate between minHeight and minHeight+the max noise) and multiply
        // it by strength so that a fraction of the result that gets used can be decided
        float heightToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
        heightToAdd += minHeight;
        heightToAdd = heightToAdd * strength;

        SetBlocks(chunk, x, z, (int)heightSoFar, (int)(heightSoFar + heightToAdd), blockToPlace);

        return heightSoFar + heightToAdd;
    }
}
