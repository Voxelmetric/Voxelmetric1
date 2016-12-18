using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities.Noise;


public class CavesLayer : TerrainLayer
{
    protected override void SetUp(LayerConfig config)
    {
        // Doesn't currently support customization via config but you can add them like this:
        // frequency = float.Parse(properties["frequency"]);
        // and it will fetch an element in the config's property object called frequency
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        float caveBottom = NoiseUtils.GetNoise(noise.Noise, x+chunk.pos.x, -1000.0f, z+chunk.pos.z, 500.0f, 70, 1.0f);
        float caveHeight = NoiseUtils.GetNoise(noise.Noise, x + chunk.pos.x, 1000.0f, z+chunk.pos.z, 50.0f, 30, 1.0f) + caveBottom;

        caveHeight -= 20f;

        if (caveHeight > caveBottom)
        {
            caveBottom -= caveHeight / 2f;
            float caveTop = caveHeight / 2f;
            if (caveTop > heightSoFar && caveBottom < heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        float caveBottom = NoiseUtils.GetNoise(noise.Noise, x + chunk.pos.x, -1000.0f, z+chunk.pos.z, 500.0f, 70, 1.0f);
        float caveHeight = NoiseUtils.GetNoise(noise.Noise, x + chunk.pos.x, 1000.0f, z+chunk.pos.z, 50.0f, 30, 1.0f) + caveBottom;

        caveHeight -= 20;

        if (caveHeight > caveBottom)
        {
            caveBottom -= caveHeight / 2f;
            float caveTop = caveHeight / 2f;
            SetBlocks(chunk, x, z, (int)caveBottom, (int)caveTop, chunk.world.blockProvider.BlockTypes[BlockProvider.AirType]);

            if (caveTop > heightSoFar && caveBottom < heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }
}