using NUnit.Framework;
using Voxelmetric.Code.Core;

public class WorldTest {

    [Test]
    public void ConstructionTest() {
        World world = TestUtils.CreateWorldDefault();
        Assert.IsNotNull(world.chunks, "world.chunks");
        Assert.IsNotNull(world.blocks, "world.blocks");
        world.Configure();
        Assert.IsNotNull(world.config, "world.config");
        Assert.IsNotNull(world.config.blockFolder, "world.config.blockFolder");
        Assert.IsNotNull(world.textureIndex, "world.textureIndex");
        Assert.IsNotNull(world.blockIndex, "world.blockIndex");
    }

}
