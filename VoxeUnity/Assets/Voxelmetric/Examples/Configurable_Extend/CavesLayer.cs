using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Load_Resources.Blocks;


public class CavesLayer : TerrainLayer
{


    protected override void SetUp(LayerConfig config)
    {
        // Doesn't currently support customization via config but you can add them like this:
        // frequency = float.Parse(properties["frequency"]);
        // and it will fetch an element in the config's property object called frequency
    }

    public override int GetHeight(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        int caveBottom = GetNoise(x, -1000, z, 500, 70, 1);
        int caveHeight = GetNoise(x, 1000, z, 50, 30, 1) + caveBottom;

        caveHeight -= 20;

        if (caveHeight > caveBottom)
        {
            caveBottom -= caveHeight / 2;
            int caveTop = caveHeight / 2;
            if (caveTop > heightSoFar && caveBottom < heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }

    public override int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        int caveBottom = GetNoise(x, -1000, z, 500, 70, 1);
        int caveHeight = GetNoise(x, 1000, z, 50, 30, 1) + caveBottom;

        caveHeight -= 20;

        if (caveHeight > caveBottom)
        {
            caveBottom -= caveHeight / 2;
            int caveTop = caveHeight / 2;
            SetBlocks(chunk, x, z, caveBottom, caveTop, chunk.world.blockProvider.BlockTypes[BlockProvider.AirType]);

            if (caveTop > heightSoFar && caveBottom < heightSoFar)
                return caveBottom;
        }

        return heightSoFar;
    }
}