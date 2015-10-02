using System.Collections.Generic;

public class VoxelmetricResources {

    public World mainWorld;
    public TextureIndex textureIndex = new TextureIndex();
    public BlockIndex blockIndex = new BlockIndex();

    public VoxelmetricConfigsLoader config = new VoxelmetricConfigsLoader();

    public List<World> worlds = new List<World>();

    public void AddWorld(World world)
    {
        worlds.Add(world);
        if (worlds.Count == 1)
        {
            blockIndex.GetMissingDefinitions();
        }
    }
}
