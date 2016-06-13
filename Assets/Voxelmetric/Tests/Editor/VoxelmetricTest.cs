using NUnit.Framework;
using System.Threading;
using System.Collections.Generic;
using System;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Serialization;
using Stopwatch = System.Diagnostics.Stopwatch;

public class VoxelmetricTest {

    [Test]
    public void WorldDefaultTest()
    {
        TestWorldDefault(false);
    }

    [Test]
    public void WorldDefaultMTTest() {
        TestWorldDefault(true);
    }

    [Test]
    public void WorldColoredTest() {
        TestWorldColored(false);
    }

    [Test]
    public void WorldColoredMTTest() {
        TestWorldColored(true);
    }

    [Test]
    public void NetworkingTest() {
        // NOTE: VmClient and VmServer are threaded anyway
        //bool useMultiThreading = false; // Config.Toggle.UseMultiThreadingDefault
        //bool debug = false;

        //List<World> worlds = new List<World>();
        //try {
        //    // Start the server
        //    World serverWorld = TestUtils.CreateWorldDefault();
        //    worlds.Add(serverWorld);
        //    serverWorld.networking.isServer = true;
        //    serverWorld.networking.allowConnections = true;
        //    serverWorld.useMultiThreading = useMultiThreading;
        //    serverWorld.StartWorld();

        //    IPAddress serverIP = serverWorld.networking.server.ServerIP;
        //    Assert.IsNotNull(serverIP, "serverIP");

        //    // Start the client and connect to the server
        //    World clientWorld = TestUtils.CreateWorldDefault();
        //    worlds.Add(clientWorld);
        //    clientWorld.networking.isServer = false;
        //    clientWorld.networking.serverIP = serverIP;
        //    clientWorld.useMultiThreading = useMultiThreading;
        //    clientWorld.StartWorld();

        //    // Wait a short while for the client and server to get done initializing
        //    Thread.Sleep(100); // 100 ms should be plenty of time

        //    // At this point there should be nothing loaded either on client or server
        //    Assert.AreEqual(0, serverWorld.chunks.chunkCollection.Count, "serverWorld chunk count");
        //    Assert.AreEqual(0, clientWorld.chunks.chunkCollection.Count, "clientWorld chunk count");

        //    // Cause a chunk to load on the server
        //    BlockPos loadPos = new BlockPos(0, 0, 0);
        //    serverWorld.chunks.New(loadPos);

        //    // Wait a short while for the server to get done loading
        //    Thread.Sleep(100); // 100 ms should be plenty of time

        //    if (useMultiThreading) {
        //        // Need to just wait for the threads to do their work
        //        Thread.Sleep(100); // 100 ms should be plenty of time
        //    } else {
        //        serverWorld.chunksLoop.UpdateTerrain();
        //    }

        //    // At this point the server should be loaded, but there should be nothing loaded on the client
        //    Assert.AreEqual(27, serverWorld.chunks.chunkCollection.Count, "serverWorld chunk count");
        //    Assert.AreEqual(0, clientWorld.chunks.chunkCollection.Count, "clientWorld chunk count");

        //    if (debug) {
        //        TestUtils.DebugBlockCounts("server", TestUtils.FindWorldBlockCounts(serverWorld));
        //        TestUtils.DebugBlockCounts("client", TestUtils.FindWorldBlockCounts(clientWorld));
        //    }

        //    if (debug) Debug.Log("About to create client chunk (" + Thread.CurrentThread.ManagedThreadId + "): ");

        //    // Cause the same chunk to load on the client
        //    Stopwatch stopwatch = Stopwatch.StartNew();
        //    clientWorld.chunks.New(loadPos);

        //    if (useMultiThreading) {
        //        // Need to just wait for the threads to do their work
        //        Thread.Sleep(100); // 100 ms should be plenty of time
        //    } else {
        //        clientWorld.chunksLoop.Terrain();
        //    }

        //    // Wait for the client and server to get done loading
        //    TimeSpan timeout = new TimeSpan(0, 0, 10); // 10s timeout -- might be too short for some hardware
        //    bool allLoaded;
        //    do {
        //        allLoaded = false;
        //        int numLoaded = 0;
        //        foreach (var chunk in clientWorld.chunks.chunkCollection) {
        //            if (chunk.blocks.contentsGenerated)
        //                numLoaded++;
        //        }
        //        if (debug) Debug.Log("Loaded " + numLoaded + " in " + stopwatch.Elapsed);
        //        if ( numLoaded == 27 )
        //            allLoaded = true;
        //        else
        //            Thread.Sleep(100);
        //        if (stopwatch.Elapsed > timeout) {
        //            StringBuilder sb = new StringBuilder();
        //            foreach (var chunk in clientWorld.chunks.chunkCollection) {
        //                if (chunk.blocks.contentsGenerated)
        //                    sb.AppendLine(chunk.pos.ToString());
        //            }
        //            Assert.Fail("Timed out after loading " + numLoaded + " chunks: " + sb.ToString());
        //        }
        //    } while (!allLoaded);
        //    // Taking about 6s on my machine...
        //    if (debug) Debug.Log("Loading took " + stopwatch.Elapsed);

        //    if (debug) Debug.Log("About to look at results (" + Thread.CurrentThread.ManagedThreadId + "): ");

        //    // At this point both the server and the client should be loaded
        //    Assert.AreEqual(27, serverWorld.chunks.chunkCollection.Count, "serverWorld chunk count");
        //    Assert.AreEqual(serverWorld.chunks.chunkCollection.Count, clientWorld.chunks.chunkCollection.Count, "clientWorld chunk count");
        //    if (debug) {
        //        TestUtils.DebugChunks("server", serverWorld.chunks.chunkCollection);
        //        TestUtils.DebugChunks("client", clientWorld.chunks.chunkCollection);
        //    }

        //    // Check that the server generated the expected world
        //    var serverBlockCounts = TestUtils.FindWorldBlockCounts(serverWorld);
        //    if (debug) TestUtils.DebugBlockCounts("server", serverBlockCounts);
        //    foreach (var expCount in GetExpDefaultBlocks()) {
        //        Assert.IsTrue(serverBlockCounts[expCount.Key] > expCount.Value, expCount.Key + " total not > " + expCount.Value + " was " + serverBlockCounts[expCount.Key]);
        //    }

        //    // Check that the client and server have the same worlds loaded
        //    var clientBlockCounts = TestUtils.FindWorldBlockCounts(clientWorld);
        //    if (debug) TestUtils.DebugBlockCounts("client", clientBlockCounts);
        //    foreach (var serverChunk in serverWorld.chunks.chunkCollection) {
        //        string msg = "serverChunk at " + serverChunk.pos;
        //        Chunk clientChunk = clientWorld.chunks.Get(serverChunk.pos);
        //        Assert.IsNotNull(clientChunk, msg + " has no corresponding client chunk");
        //        var clientChunkBlockCounts = TestUtils.FindChunkBlockCounts(clientChunk);
        //        var serverChunkBlockCounts = TestUtils.FindChunkBlockCounts(serverChunk);
        //        foreach (var serverPair in serverChunkBlockCounts) {
        //            int clientCount;
        //            Assert.IsTrue(clientChunkBlockCounts.TryGetValue(serverPair.Key, out clientCount), msg + " has no corresponfing client count for " + serverPair.Key);
        //            Assert.AreEqual(serverPair.Value, clientCount, msg + " count of " + serverPair.Key);
        //        }
        //        foreach (BlockPos localPos in TestUtils.LocalPosns) {
        //            Block serverBlock = serverChunk.blocks.LocalGet(localPos);
        //            Block clientBlock = clientChunk.blocks.LocalGet(localPos);
        //            Assert.AreEqual(serverBlock.type, clientBlock.type, msg + " block at " + localPos
        //                + " has no matching clientBlock type");
        //        }
        //    }

        //    //TODO cause a chunk to load on the client (away from the server load)

        //    //TODO Change a whole bunch of blocks at once

        //    //TODO Make a whole bunch of clients join at once

        //} finally {
        //    foreach(World world in worlds)
        //        world.StopWorld();
        //}
    }

    private Dictionary<string, int> GetExpDefaultBlocks() {
        return new Dictionary<string, int> {
            { "stone", 10000 }, // stone = 14581
            { "dirt", 5000 },   // dirt = 7060
            { "grass", 1000 },  // grass = 2304
            { "air", 50000 },   // air = 85901
            { "wildgrass", 100 },   // wildgrass = 182
            { "leaves", 400 },  // leaves = 516
            { "log", 20 },      // log = 48
        };
    }

    private Dictionary<string, int> GetExpColoredBlocks() {
        return new Dictionary<string, int> {
            { "coloredblock", 10000 }, // coloredblock = 23945
            { "air", 50000 },   // air = 86647
        };
    }

    private void TestWorldDefault(bool useMultiThreading) {
        World world = TestUtils.CreateWorldDefault();
        AssertWorld(world, useMultiThreading, GetExpDefaultBlocks(), "log", "air", "stone");
    }

    private void TestWorldColored(bool useMultiThreading) {
        World world = TestUtils.CreateWorldColored();
        AssertWorld(world, useMultiThreading, GetExpColoredBlocks(), "coloredblock", "air", "coloredblock");
    }

    private static void AppendBlockPosns(Chunk chunk, string blockName, List<BlockPos> blockPosns) {
        foreach (BlockPos localPos in TestUtils.LocalPosns) {
            Block block = chunk.blocks.GetBlock(localPos);
            if (block.name == blockName) {
                blockPosns.Add(chunk.pos + localPos);
            }
        }
    }

    private static void AssertWorld(World world, bool useMultiThreading,
            Dictionary<string, int> expBlockCounts,
            string editName, string airName, string placeName) {
        //bool debug = false;

        //try {
        //    world.useMultiThreading = useMultiThreading;
        //    world.StartWorld();

        //    // Cause a chunk to load
        //    BlockPos loadPos = new BlockPos(0, 0, 0);
        //    world.chunks.New(loadPos);

        //    var stageChunks = new TestUtils.AutoDictionary<Stage, List<Chunk>>();
        //    var blockCounts = new TestUtils.AutoDictionary<string, int>();
        //    List<BlockPos> editPosns = new List<BlockPos>();
        //    if (useMultiThreading) {
        //        // Need to just wait for the threads to do their work
        //        Thread.Sleep(100); // 100 ms should be plenty of time

        //        // Run for a while to load some chunks
        //        //for (int i = 0; i<100; ++i)
        //        //    world.chunksLoop.MainThreadLoop();
        //    } else {
        //        // This call should now cause a bunch of chunks to get generated
        //        // and one to move into BuildMesh
        //        world.chunksLoop.Terrain();

        //        // TODO TCD test MainThreadLoop earlier and later too
        //        world.chunksLoop.MainThreadLoop(); // Should do nothing when called here
        //    }

        //    // Check that things have been generated as expected
        //    foreach (var chunk in world.chunks.chunkCollection) {
        //        stageChunks[chunk.stage].Add(chunk);
        //        AppendBlockPosns(chunk, editName, editPosns);
        //        foreach (var pair in TestUtils.FindChunkBlockCounts(chunk))
        //            blockCounts[pair.Key] += pair.Value;
        //    }

        //    if (!useMultiThreading) {
        //        Assert.AreEqual(2, stageChunks.Count, "Terrain stageChunks.Count");
        //        Assert.AreEqual(26, stageChunks[Stage.created].Count, "Terrain stageChunks[Stage.created]");
        //        Assert.AreEqual(1, stageChunks[Stage.buildMesh].Count, "Terrain stageChunks[Stage.buildMesh]");
        //        Chunk buildMeshChunk = stageChunks[Stage.buildMesh].FirstOrDefault();
        //        Assert.AreEqual(loadPos.ContainingChunkCoordinates(), buildMeshChunk.pos, "Terrain chunk at loadPos should be at buildMesh");
        //    }

        //    if (debug) TestUtils.DebugBlockCounts(useMultiThreading ? "multithreading" : "single thread", blockCounts);
        //    foreach (var expCount in expBlockCounts) {
        //        Assert.IsTrue(blockCounts[expCount.Key] > expCount.Value, expCount.Key + " total not > " + expCount.Value + " was " + blockCounts[expCount.Key]);
        //    }

        //    // Save before anything is changed
        //    SaveProgress saveProgress = Voxelmetric.SaveAll(world);
        //    if (useMultiThreading)
        //        AssertWaitForSave(saveProgress, new TimeSpan(0, 0, 2)); // 2s should be way long enough

        //    //TODO Check for dirty chunk (should be 0 here)
        //    Assert.AreEqual(27, saveProgress.totalChunksToSave);
        //    Assert.AreEqual(100, saveProgress.GetProgress());
        //    Assert.AreEqual(0, saveProgress.ErrorChunks.Count());

        //    if (useMultiThreading) {
        //        // Need to just wait for the threads to do their work
        //        Thread.Sleep(100); // 100 ms should be plenty of time

        //        // Run for a while to load some chunks
        //        //for (int i = 0; i<100; ++i)
        //        //    world.chunksLoop.MainThreadLoop();
        //    } else {
        //        // Generate a mesh
        //        world.chunksLoop.BuildMesh();
        //    }

        //    // Update test info
        //    stageChunks.Clear();
        //    foreach (var chunk in world.chunks.chunkCollection)
        //        stageChunks[chunk.stage].Add(chunk);

        //    // Check that meshes got generated
        //    Assert.AreEqual(2, stageChunks.Count, "BuildMesh stageChunks.Count");
        //    Assert.AreEqual(26, stageChunks[Stage.created].Count, "BuildMesh stageChunks[Stage.created]");
        //    Assert.AreEqual(1, stageChunks[Stage.render].Count, "BuildMesh stageChunks[Stage.render]");
        //    Chunk renderChunk = stageChunks[Stage.render].FirstOrDefault();
        //    Assert.AreEqual(loadPos.ContainingChunkCoordinates(), renderChunk.pos, "BuildMesh chunk at loadPos should be at render");

        //    // Check that editPosns have editName
        //    foreach (var pos in editPosns) {
        //        Block block = Voxelmetric.GetBlock(pos, world);
        //        Assert.AreEqual(editName, block.name);
        //    }

        //    // Change a block to air
        //    Voxelmetric.SetBlock(editPosns[0], Block.Create(airName, world), world);

        //    // Place a block
        //    Voxelmetric.SetBlock(editPosns[1], Block.Create(placeName, world), world);

        //    // Save again
        //    saveProgress = Voxelmetric.SaveAll(world);
        //    if (useMultiThreading)
        //        AssertWaitForSave(saveProgress, new TimeSpan(0, 0, 2)); // 2s should be way long enough

        //    //TODO Check for dirty chunk (should be >0 here)
        //    Assert.AreEqual(27, saveProgress.totalChunksToSave);
        //    Assert.AreEqual(100, saveProgress.GetProgress());
        //    Assert.AreEqual(0, saveProgress.ErrorChunks.Count());

        //    // Load
        //    // TODO TCD



        //    // Check that edits change chunk stages as expected
        //    stageChunks.Clear();
        //    foreach (var chunk in world.chunks.chunkCollection)
        //        stageChunks[chunk.stage].Add(chunk);
        //    if (debug) TestUtils.DebugChunks("final chunks", world.chunks.chunkCollection);
        //    if (useMultiThreading) {
        //        Assert.AreEqual(2, stageChunks.Count, "Edit stageChunks.Count");
        //        Assert.AreEqual(25, stageChunks[Stage.created].Count, "Edit stageChunks[Stage.created]");
        //        Assert.AreEqual(2, stageChunks[Stage.render].Count, "Edit stageChunks[Stage.render]");
        //    } else {
        //        Assert.AreEqual(3, stageChunks.Count, "Edit stageChunks.Count");
        //        Assert.AreEqual(25, stageChunks[Stage.created].Count, "Edit stageChunks[Stage.created]");
        //        Assert.AreEqual(1, stageChunks[Stage.priorityBuildMesh].Count, "Edit stageChunks[Stage.priorityBuildMesh]");
        //        Assert.AreEqual(1, stageChunks[Stage.render].Count, "Edit stageChunks[Stage.render]");
        //        Chunk buildMeshChunk = stageChunks[Stage.priorityBuildMesh].FirstOrDefault();
        //        Assert.AreEqual(editPosns[0].ContainingChunkCoordinates(), buildMeshChunk.pos, "Edit chunk at editPosns[0] should be at priorityBuildMesh");
        //        Assert.AreEqual(editPosns[1].ContainingChunkCoordinates(), buildMeshChunk.pos, "Edit chunk at editPosns[1] should be at priorityBuildMesh");
        //    }

        //    // TODO TCD Generate another mesh
        //    //world.chunksLoop.BuildMesh();
        //} finally {
        //    world.StopWorld();
        //}
    }

    private static void AssertWaitForSave(SaveProgress saveProgress, TimeSpan timeout) {
        Stopwatch stopwatch = Stopwatch.StartNew();
        while (saveProgress.GetProgress() < 100) {
            Thread.Sleep(10);
            if (stopwatch.Elapsed > timeout)
                Assert.Fail("SaveAll took too long");
        }
    }
}
