using UnityEngine;
using UnityEditor;
using NUnit.Framework;

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

    [Test]
    public void BasicTest() {
        World world = TestUtils.CreateWorldDefault();
        world.UseMultiThreading = false;
        world.UseCoroutines = false;
        world.StartWorld();

        // Cause a chunk to load and then generate terrain
        BlockPos loadPos = new BlockPos(0, 0, 0);
        world.chunks.New(loadPos);
        world.chunksLoop.Terrain();

        // Verify that the terrain is there
        var startPos = new BlockPos(8, 0, 8);
        Assert.IsTrue(world.FindGroundPos(ref startPos, 1));
        Assert.AreEqual(new BlockPos(8, -10, 8), startPos);
    }
}
