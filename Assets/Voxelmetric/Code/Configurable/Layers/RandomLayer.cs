using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;

public class RandomLayer: TerrainLayer
{
    Block blockToPlace;
    float chance;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for random layers MUST define these properties
        blockToPlace = Block.Create(properties["blockName"], world);

        if (properties.ContainsKey("blockColors"))
        {
            string[] colors = properties["blockColors"].Split(',');
            ((ColoredBlock)blockToPlace).color = new Color(byte.Parse(colors[0]) / 255f, byte.Parse(colors[1]) / 255f, byte.Parse(colors[2]) / 255f);
        }

        chance = float.Parse(properties["chance"]);
    }

    public override int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        float posChance = new BlockPos(x, heightSoFar + 1, z).Random(200);

        if (chance > posChance)
        {
            if (!justGetHeight)
                SetBlocksColumn(chunk, x, z, heightSoFar, heightSoFar + 1, blockToPlace);

            return heightSoFar + 1;
        }
        else
        {
            return heightSoFar;
        }
    }
}