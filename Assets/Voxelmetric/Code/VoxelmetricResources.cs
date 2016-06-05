using System.Collections.Generic;

public class VoxelmetricResources {

    public System.Random random = new System.Random();

    // Worlds can use different block and texture indexes or share them so they are mapped here to
    // the folders they're loaded from so that a world can create a new index or if it uses the same
    // folder to fetch and index it can return an existing index and avoid building it again

    /// <summary>
    /// A map of texture indexes with the folder they're built from
    /// </summary>
    public Dictionary<string, TextureIndex> textureIndexes = new Dictionary<string, TextureIndex>();

    public TextureIndex GetOrLoadTextureIndex(World world)
    {
        //Check for the folder in the dictionary and if it doesn't exist create it
        TextureIndex textureIndex;
        if (textureIndexes.TryGetValue(world.config.textureFolder, out textureIndex))
        {
            return textureIndex;
        }

        textureIndex = new TextureIndex(world.config);
        textureIndexes.Add(world.config.textureFolder, textureIndex);
        return textureIndex;
    }

    /// <summary>
    /// A map of block indexes with the folder they're built from
    /// </summary>
    public Dictionary<string, BlockIndex> blockIndexes = new Dictionary<string, BlockIndex>();

    public BlockIndex GetOrLoadBlockIndex(World world)
    {
        //Check for the folder in the dictionary and if it doesn't exist create it
        BlockIndex blockIndex;
        if (blockIndexes.TryGetValue(world.config.blockFolder, out blockIndex))
        {
            return blockIndex;
        }

        blockIndex = new BlockIndex(world.config.blockFolder, world);
        blockIndexes.Add(world.config.blockFolder, blockIndex);
        return blockIndex;
    }

}
