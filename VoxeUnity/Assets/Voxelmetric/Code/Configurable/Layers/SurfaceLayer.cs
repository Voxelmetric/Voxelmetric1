using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;

public class SurfaceLayer : TerrainLayer
{
    // Right now this acts just like additive layer but always set to one block thickness
    // but it's a placeholder so that in the future we can do things like blend surface layers
    // between separate biomes

    Block blockToPlace;

    protected override void SetUp(LayerConfig config)
    {
        blockToPlace = world.blockProvider.GetBlock(properties["blockName"]);

        if (properties.ContainsKey("blockColors"))
        {
            string[] colors = properties["blockColors"].Split(',');
            ((ColoredBlock)blockToPlace).color = new Color(byte.Parse(colors[0]) / 255f, byte.Parse(colors[1]) / 255f, byte.Parse(colors[2]) / 255f);
        }
    }

    public override int GetHeight(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        return heightSoFar + 1;
    }

    public override int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        SetBlocks(chunk, x, z, heightSoFar, heightSoFar + 1, blockToPlace);

        return heightSoFar + 1;
    }
}