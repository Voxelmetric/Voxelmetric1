using System.Globalization;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;

public class BiomeLayer : TerrainLayer
{
    private BlockData primaryBlockToSpawn; // Surface block (i.e: grass, sand)
    private BlockData secondaryBlockToSpawn; // Subterrain block (i.e: dirt)
    private BlockData coastalBlockToSpawn; // Coastal block (i.e: sand)
    private BlockData seaBlockToSpawn; // Sea block (i.e: water)

    private int primaryBlockDepth; // The amount of primary blocks that will spawn underneath
    private int secondaryBlockDepth; // The amount of secondary blocks that will spawn underneath
    private int coastalBlockDepth; // The amount of coastal blocks that will spawn underneath

    private float minTemp; // Minimum temperature this biome will spawn (0-10)
    private float maxTemp; // Maximum temperature this biome will spawn (0-10)
    private float minHum; // Minimum humidity this biome will spawn (0-10)
    private float maxHum; // Maximum humidity this biome will spawn (0-10)

    private int coastSize; // The amount of blocks away from the sea that the coast is allowed to spawn
    private int coastalLevel; // The maximum height of the coast (seaLevel + coastSize)

    private const int seaLevel = 200; // The maximum height of the sea

    protected override void SetUp(LayerConfig config)
    {
        Block primaryBlock = world.blockProvider.GetBlock(properties["primaryBlockName"]);
        primaryBlockToSpawn = new BlockData(primaryBlock.Type, primaryBlock.Solid);

        Block secondaryBlock = world.blockProvider.GetBlock(properties["secondaryBlockName"]);
        secondaryBlockToSpawn = new BlockData(secondaryBlock.Type, secondaryBlock.Solid);

        Block coastalBlock = world.blockProvider.GetBlock(properties["coastalBlockName"]);
        coastalBlockToSpawn = new BlockData(coastalBlock.Type, coastalBlock.Solid);

        Block seaBlock = world.blockProvider.GetBlock(properties["seaBlockName"]);
        seaBlockToSpawn = new BlockData(seaBlock.Type, seaBlock.Solid);

        primaryBlockDepth = int.Parse(properties["primaryBlockDepth"], CultureInfo.InvariantCulture);
        secondaryBlockDepth = int.Parse(properties["secondaryBlockDepth"], CultureInfo.InvariantCulture);
        coastalBlockDepth = int.Parse(properties["coastalBlockDepth"], CultureInfo.InvariantCulture);

        coastSize = int.Parse(properties["coastSize"], CultureInfo.InvariantCulture);

        minTemp = float.Parse(properties["minTemp"], CultureInfo.InvariantCulture);
        maxTemp = float.Parse(properties["maxTemp"], CultureInfo.InvariantCulture);
        minHum = float.Parse(properties["minHum"], CultureInfo.InvariantCulture);
        maxHum = float.Parse(properties["maxHum"], CultureInfo.InvariantCulture);

        coastalLevel = seaLevel + coastSize;
    }

    public override float GetTemperature(Chunk chunk, int layerIndex, int x, int z, float tempSoFar)
    {
        return tempSoFar;
    }

    public override float GetHumidity(Chunk chunk, int layerIndex, int x, int z, float humSoFar)
    {
        return humSoFar;
    }

    public override float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar)
    {
        return heightSoFar + 1;
    }

    public override float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float tempSoFar, float humSoFar)
    {
        if (tempSoFar > minTemp && tempSoFar < maxTemp && humSoFar > minHum && humSoFar < maxHum) // Can we generate the biome here?
        {
            if (heightSoFar > coastalLevel) // Is this block above the coast level?
            {
                SetBlocks(chunk, x, z, (int)heightSoFar, (int)heightSoFar + secondaryBlockDepth, secondaryBlockToSpawn); // Spawn the secondary block first
                SetBlocks(chunk, x, z, (int)heightSoFar + secondaryBlockDepth, (int)heightSoFar + secondaryBlockDepth + primaryBlockDepth, primaryBlockToSpawn); // Spawn the primary block on top
            }
            else // If we're at or below coastal level..
            {
                SetBlocks(chunk, x, z, (int)heightSoFar, seaLevel + 1, seaBlockToSpawn); // Spawn the sea block
                SetBlocks(chunk, x, z, (int)heightSoFar, (int)heightSoFar + coastalBlockDepth + primaryBlockDepth, coastalBlockToSpawn); // Spawn the coastal block
            }
        }
        return heightSoFar + 1;
    }
}