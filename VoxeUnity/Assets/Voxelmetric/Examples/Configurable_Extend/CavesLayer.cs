using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities.Noise;


public class CavesLayer: TerrainLayer
{
    protected override void SetUp(LayerConfig config)
    {
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        int caveBottom = (int)NoiseUtils.GetNoise(noise.Noise, x+chunk.pos.x, -1000f, z+chunk.pos.z, 500f, 70, 1f)+
                           world.config.minY;
        int caveHeight = (int)NoiseUtils.GetNoise(noise.Noise, x+chunk.pos.x, 1000f, z+chunk.pos.z, 50f, 30, 1f)+caveBottom;

        caveHeight -= 10;

        if (caveHeight>caveBottom)
        {
            caveBottom -= (caveHeight>>1);
            int caveTop = (caveHeight>>1);
            if (caveTop>heightSoFar && caveBottom<heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        int caveBottom = (int)NoiseUtils.GetNoise(noise.Noise, x+chunk.pos.x, -1000f, z+chunk.pos.z, 500f, 70, 1f)+
                           world.config.minY;
        int caveHeight = (int)NoiseUtils.GetNoise(noise.Noise, x+chunk.pos.x, 1000f, z+chunk.pos.z, 50f, 30, 1f)+caveBottom;

        caveHeight -= 10;

        if (caveHeight>caveBottom)
        {
            caveBottom -= (caveHeight>>1);
            int caveTop = (caveHeight>>1);
            SetBlocks(chunk, x, z, caveBottom, caveTop, BlockProvider.AirBlock);

            if (caveTop>heightSoFar && caveBottom<heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }
}