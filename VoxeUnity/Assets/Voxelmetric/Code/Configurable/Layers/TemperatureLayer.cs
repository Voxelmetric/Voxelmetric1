using System.Globalization;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities.Noise;

public class TemperatureLayer : TerrainLayer
{ 
	private float minTemp;

	protected override void SetUp(LayerConfig config)
    {
		minTemp = float.Parse(properties["minTemp"], CultureInfo.InvariantCulture);
	}

	public override void PreProcess(Chunk chunk, int layerIndex)
    {
		var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
		var ni = pools.noiseItems[layerIndex];
		ni.noiseGen.SetInterpBitStep(Env.ChunkSizeWithPadding, 2);
		ni.lookupTable = pools.FloatArrayPool.Pop(ni.noiseGen.Size*ni.noiseGen.Size);

		#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
		float[] noiseSet = chunk.pools.FloatArrayPool.Pop(ni.noiseGen.Size * ni.noiseGen.Size * ni.noiseGen.Size);

		// Generate SIMD noise
		int offsetShift = Env.ChunkPow - ni.noiseGen.Step;
		int xStart = (chunk.Pos.x * Env.ChunkSize) << offsetShift;
		int yStart = (chunk.Pos.y * Env.ChunkSize) << offsetShift;
		int zStart = (chunk.Pos.z * Env.ChunkSize) << offsetShift;
		float scaleModifier = 1 << ni.noiseGen.Step;
		noiseSIMD.Noise.FillNoiseSet(noiseSet, xStart, yStart, zStart, ni.noiseGen.Size, ni.noiseGen.Size, ni.noiseGen.Size, scaleModifier);

		// Generate a lookup table
		int i = 0;
		for (int z = 0; z < ni.noiseGen.Size; z++)
		for (int x = 0; x < ni.noiseGen.Size; x++)
		ni.lookupTable[i++] = NoiseUtilsSIMD.GetNoise(noiseSet, ni.noiseGen.Size, x, 0, z, amplitude, noise.Gain);

		chunk.pools.FloatArrayPool.Push(noiseSet);
		#else
		int xOffset = chunk.Pos.x;
		int zOffset = chunk.Pos.z;

		// Generate a lookup table
		int i = 0;
		for (int z = 0; z<ni.noiseGen.Size; z++)
		{
			float zf = (z<<ni.noiseGen.Step)+zOffset;

			for (int x = 0; x<ni.noiseGen.Size; x++)
			{
				float xf = (x<<ni.noiseGen.Step)+xOffset;
				ni.lookupTable[i++] = NoiseUtils.GetNoise(noise.Noise, xf, -6632, zf, 75f, 100, noise.Gain);
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

	public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar)
	{
		return heightSoFar;
	}

	public override float GetTemperature(Chunk chunk, int layerIndex, int x, int z, float tempSoFar)
	{
		var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
		var ni = pools.noiseItems[layerIndex];

		float tempToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
		tempToAdd += minTemp;

		if (tempToAdd > tempSoFar)
			return tempToAdd;
			
		return tempSoFar;
	}

	public override float GetHumidity(Chunk chunk, int layerIndex, int x, int z, float humSoFar)
	{
		return humSoFar;
	}
		
	public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float tempSoFar, float humSoFar)
	{
		var pools = Globals.WorkPool.GetPool(chunk.ThreadID);
		var ni = pools.noiseItems[layerIndex];

		float tempToAdd = ni.noiseGen.Interpolate(x, z, ni.lookupTable);
		tempToAdd += minTemp;

		if (tempToAdd > tempSoFar)
			return tempToAdd;

		return tempSoFar;
	}
}