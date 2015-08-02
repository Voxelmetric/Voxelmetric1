using UnityEngine;
using System.Collections;
using SimplexNoise;

public class LayerOverride
{

    public TerrainLayer.LayerType layerType = TerrainLayer.LayerType.Absolute;

    public int baseHeight = 0;
    public int frequency = 10;
    public int amplitude = 10;
    public float exponent = 1;
    public string blockName = "stone";

    public int percentage = 90;
    public GeneratedStructure structure;
    public int chanceToSpawnBlock = 10;

    public World world;
    public Noise noiseGen;

    public virtual int GenerateLayer(int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        return heightSoFar;
    }

    public virtual void GenerateStructures(BlockPos chunkPos, TerrainGen terrainGen)
    {

    }

    public int GetNoise(int x, int y, int z, float scale, int max, float power)
    {
        float noise = (noiseGen.Generate(x / scale, y / scale, z / scale) + 1f) * (max / 2f);

        noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }
}
