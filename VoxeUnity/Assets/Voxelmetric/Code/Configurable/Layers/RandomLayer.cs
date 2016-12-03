using Assets.Voxelmetric.Code.Common.Math;
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
        blockToPlace = world.blockProvider.GetBlock(properties["blockName"]);

        if (properties.ContainsKey("blockColors"))
        {
            string[] colors = properties["blockColors"].Split(',');
            ((ColoredBlock)blockToPlace).color = new Color(byte.Parse(colors[0]) / 255f, byte.Parse(colors[1]) / 255f, byte.Parse(colors[2]) / 255f);
        }

        chance = float.Parse(properties["chance"]);
    }

    public override int GetHeight(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        var lpos = new Vector3Int(x, heightSoFar + 1, z);
        float posChance = Randomization.Random(lpos.GetHashCode(), 200);

        if (chance > posChance)
        {
            return heightSoFar + 1;
        }

        return heightSoFar;
    }

    public override int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        var lpos = new Vector3Int(x, heightSoFar+1, z);
        float posChance = Randomization.Random(lpos.GetHashCode(), 200);

        if (chance > posChance)
        {
            SetBlocks(chunk, x, z, heightSoFar, heightSoFar + 1, blockToPlace);

            return heightSoFar + 1;
        }

        return heightSoFar;
    }
}