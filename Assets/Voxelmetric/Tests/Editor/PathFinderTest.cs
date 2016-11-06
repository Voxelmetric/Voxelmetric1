using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Threading;

public class PathFinderTest {

    /// <summary>
    /// Makes a world with all chunks populated with air above y and blockType at and below y
    /// </summary>
    /// <param name="y">y position to populate with stone</param>
    /// <returns></returns>
    public static World MakePlanarWorld(int y, string blockType) {
        World world = TestUtils.CreateWorldDefault();
        world.UseMultiThreading = false;
        world.UseCoroutines = false;
        world.StartWorld();

        ushort sType = world.blockIndex.GetBlockType(blockType);

        // Cause chunks to load and then generate terrain
        BlockPos loadPos = new BlockPos(0, 0, 0);
        world.chunks.New(loadPos);
        foreach(Chunk chunk in world.chunks.chunkCollection) {
            foreach(BlockPos lPos in Chunk.LocalPosns) {
                Block block;
                if(chunk.pos.y + lPos.y > y)
                    block = Block.New(Block.AirType, world);
                else
                    block = Block.New(sType, world);
                chunk.blocks.LocalSet(lPos, block);
            }
        }
        return world;
    }

    [Test]
    public void LineTest() {
        World world = MakePlanarWorld(-10, "stone");

        int stX = 8, enX = 14, expY = -10, z = 8;
        var startPos = new BlockPos(stX, 0, z);
        Assert.IsTrue(world.FindGroundPos(ref startPos, 1));
        Assert.AreEqual(new BlockPos(stX, expY, z), startPos);

        var endPos = new BlockPos(enX, 0, z);
        Assert.IsTrue(world.FindGroundPos(ref endPos, 1));
        Assert.AreEqual(new BlockPos(enX, expY, z), endPos);

        // Check the ground is flat between
        for(int x = stX; x <= enX; ++x) {
            var pos = new BlockPos(x, 0, z);
            Assert.IsTrue(world.FindGroundPos(ref pos, 1));
            Assert.AreEqual(new BlockPos(x, expY, z), pos);
        }

        // Find a path
        var pf = new PathFinder(startPos.Add(Direction.up), endPos.Add(Direction.up), world);
        pf.FindPath();
        Assert.AreEqual(PathFinder.Status.succeeded, pf.status);
        Assert.AreEqual(enX - stX, pf.path.Count);
        for(int i = 0; i < pf.path.Count; ++i) {
            Assert.AreEqual(new BlockPos(stX + i + 1, expY + 1, z), pf.path[i]);
        }

        // Find shorter paths
        for(int x = enX - 1; x > stX; --x) {
            endPos = new BlockPos(x, expY, z);
            pf = new PathFinder(startPos.Add(Direction.up), endPos.Add(Direction.up), world);
            pf.FindPath();
            Assert.AreEqual(PathFinder.Status.succeeded, pf.status);
            Assert.AreEqual(x - stX, pf.path.Count, "x=" + x);
            for(int i = 0; i < pf.path.Count; ++i) {
                Assert.AreEqual(new BlockPos(stX + i + 1, expY + 1, z), pf.path[i]);
            }
        }

        // Special case -- path where start == end still has length 1
        pf = new PathFinder(startPos.Add(Direction.up), startPos.Add(Direction.up), world);
        pf.FindPath();
        Assert.AreEqual(PathFinder.Status.succeeded, pf.status);
        Assert.AreEqual(1, pf.path.Count);
        Assert.AreEqual(new BlockPos(stX, expY + 1, z), pf.path[0]);
    }

    /// <summary>
    /// Test that a 45 degree set of steps can have a path found up them
    /// </summary>
    [Test]
    public void ClimbTest() {
        int gndY = -10;
        string solidType = "stone";
        World world = MakePlanarWorld(gndY, solidType);
        
        // Add a slope to climb
        int stX = 0, z = 4, sDist = 5, enX = stX + sDist + 5, ssX = enX - sDist;
        for(int s = 0; s < sDist; ++s) {
            world.SetBlock(new BlockPos(ssX + s, gndY + s, z), solidType);
        }
        world.SetBlock(new BlockPos(enX, gndY + sDist, z), solidType);

        var startPos = new BlockPos(stX, gndY + 1, z);
        var endPos = new BlockPos(enX, gndY + sDist + 1, z);

        // Find a path
        var pf = new PathFinder(startPos, endPos, world);
        pf.FindPath();
        Assert.AreEqual(PathFinder.Status.succeeded, pf.status);
        Assert.AreEqual(enX - stX, pf.path.Count);
        for(int i = 0; i < pf.path.Count; ++i) {
            int expX = stX + i + 1;
            int expY = expX < ssX ? gndY + 1 : gndY + (expX - ssX) + 1;
            Assert.AreEqual(new BlockPos(expX, expY, z), pf.path[i]);
        }
    }

    /// <summary>
    /// Test that a vertical wall has no path up it
    /// </summary>
    [Test]
    public void UnclimbableTest() {
        int gndY = -10;
        string solidType = "stone";
        World world = MakePlanarWorld(gndY, solidType);

        // Add a vertical wall to climb
        int stX = 0, z = 4, sHeight = 5, enX = stX + 10, ssX = enX;
        for(int s = 0; s < sHeight; ++s) {
            world.SetBlock(new BlockPos(ssX, gndY + s, z), solidType);
        }
        world.SetBlock(new BlockPos(enX, gndY + sHeight, z), solidType);

        var startPos = new BlockPos(stX, gndY + 1, z);
        var endPos = new BlockPos(enX, gndY + sHeight + 1, z);

        // Find a path
        var pf = new PathFinder(startPos, endPos, world);
        pf.FindPath();
        Assert.AreEqual(PathFinder.Status.failed, pf.status);
        Assert.IsNotNull(pf.path);
        Assert.AreEqual(0, pf.path.Count);
    }

}
