using System.Globalization;
using UnityEngine;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;

public class RandomLayer: TerrainLayer
{
    private BlockData blockToPlace;
    private float chance;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for random layers MUST define these properties
        Block block = world.blockProvider.GetBlock(properties["blockName"]);
        blockToPlace = new BlockData(block.Type, block.Solid);
        
        chance = float.Parse(properties["chance"], CultureInfo.InvariantCulture);
    }
    
    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        var lpos = new Vector3(chunk.Pos.x + x, heightSoFar + 1f, chunk.Pos.z);
        float posChance = Randomization.Random(lpos.GetHashCode(), 200);

        if (chance > posChance)
        {
            return heightSoFar + 1;
        }

        return heightSoFar;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength)
    {
        var lpos = new Vector3(chunk.Pos.x + x, heightSoFar + 1f, chunk.Pos.z);
        float posChance = Randomization.Random(lpos.GetHashCode(), 200);

        if (chance > posChance)
        {
            SetBlocks(chunk, x, z, (int)heightSoFar, (int)(heightSoFar+1f), blockToPlace);

            return heightSoFar+1;
        }

        return heightSoFar;
    }
}