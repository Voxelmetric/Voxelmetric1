using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System;

public class VmNetworkingTest {

    [Test]
    public void WorldTest() {
        // TODO TCD belongs in a WorldTest class
        World world = VoxelmetricTest.createWorldDefault();
        Assert.IsNotNull(world.chunks, "world.chunks");
        Assert.IsNotNull(world.blocks, "world.blocks");
        world.Configure();
        Assert.IsNotNull(world.config, "world.config");
        Assert.IsNotNull(world.config.blockFolder, "world.config.blockFolder");
        Assert.IsNotNull(world.textureIndex, "world.textureIndex");
        Assert.IsNotNull(world.blockIndex, "world.blockIndex");
    }

    [Test]
    public void SerializationTest()
    {
        bool debug = false;

        const int blockTypes = 2;
        var rand = new System.Random(444);

        World world = VoxelmetricTest.createWorldDefault();
        world.Configure();
        for (int type = 0; type < blockTypes; ++type) {
            var config = world.blockIndex.GetConfig(type);
            Assert.IsNotNull(config, "config");
            Assert.IsNotNull(config.blockClass, "config.blockClass");
        }

        Block[] rndBlocks = new Block[blockTypes];
        for (int type = 0; type < blockTypes; ++type) {
            Block block = new Block(type);
            block.world = world;
            rndBlocks[type] = block;
        }

        BlockPos chunkPos = new BlockPos(0, 0, 0);
        Chunk fromChunk = new Chunk(world, chunkPos);
        foreach (BlockPos localPos in VoxelmetricTest.LocalPosns)
            fromChunk.blocks.LocalSet(localPos, rndBlocks[rand.Next(2)]);
        if (debug) VoxelmetricTest.DebugBlockCounts("fromChunk", VoxelmetricTest.findChunkBlockCounts(fromChunk));

        byte[] chunkData = fromChunk.blocks.ToBytes();
        Chunk toChunk = new Chunk(world, chunkPos);

        const int headerSize = VmServer.headerSize;
        const int leaderSize = VmServer.leaderSize;

        int chunkDataIndex = 0;
        while (chunkDataIndex < chunkData.Length) {
            byte[] message = new byte[1024];
            message[0] = VmNetworking.transmitChunkData;
            chunkPos.ToBytes().CopyTo(message, 1);
            BitConverter.GetBytes(chunkDataIndex).CopyTo(message, headerSize);
            BitConverter.GetBytes(chunkData.Length).CopyTo(message, headerSize + 4);

            for (int i = leaderSize; i < message.Length; i++) {
                message[i] = chunkData[chunkDataIndex];
                chunkDataIndex++;

                if (chunkDataIndex >= chunkData.Length) {
                    break;
                }
            }

            toChunk.blocks.ReceiveChunkData(message);
        }

        // Check that toChunk has the same blocks as fromChunk
        if (debug) VoxelmetricTest.DebugBlockCounts("toChunk", VoxelmetricTest.findChunkBlockCounts(toChunk));
        foreach (BlockPos localPos in VoxelmetricTest.LocalPosns) {
            Block fromBlock = fromChunk.blocks.LocalGet(localPos);
            Block toBlock = toChunk.blocks.LocalGet(localPos);
            Assert.AreEqual(fromBlock.type, toBlock.type, "type of " + localPos);
        }
    }

    [Test]
    public void ThreadingTest()
    {
        bool debug = true;

        List<VmNetworking> networkings = new List<VmNetworking>();
        try {
            //Setup server
            VmNetworking serverNetworking = new VmNetworking();
            networkings.Add(serverNetworking);
            serverNetworking.isServer = true;
            serverNetworking.allowConnections = true;
            if (debug) Debug.Log("Starting Server (" + Thread.CurrentThread.ManagedThreadId + "): ");
            World serverWorld = VoxelmetricTest.createWorldDefault();
            serverNetworking.StartConnections(serverWorld);

            IPAddress serverIP = serverNetworking.server.ServerIP;
            Assert.IsNotNull(serverIP, "serverIP");

            // Start the client and connect to the server
            VmNetworking clientNetworking = new VmNetworking();
            networkings.Add(clientNetworking);
            clientNetworking.isServer = false;
            clientNetworking.serverIP = serverIP;
            if (debug) Debug.Log("Starting Client (" + Thread.CurrentThread.ManagedThreadId + "): ");
            World clientWorld = VoxelmetricTest.createWorldDefault();
            clientNetworking.StartConnections(clientWorld);

            // Wait a short while for the client and server to get done initializing
            Thread.Sleep(100); // 100 ms should be plenty of time

            // Have the client request a chunk
            BlockPos chunkPos = new BlockPos(0, 0, 0);
            clientNetworking.client.RequestChunk(chunkPos);

            // Wait a short while for the request to be serviced
            Thread.Sleep(100); // 100 ms should be plenty of time

            // Test that the server has had the chunk requested
            //serverWorld.

            //TODO also test client.BroadcastChange


            //TODO request many chunks in rapid succession

            //TODO broadcast many changes in rapid succession

            //TODO Start up many clients in rapid succession

        } finally {
            // Wait a short while for the async calls to settle down
            Thread.Sleep(100); // 100 ms should be plenty of time
            if (debug) Debug.Log("Ending all connections (" + Thread.CurrentThread.ManagedThreadId + "): ");
            foreach (var networking in networkings)
                networking.EndConnections();
        }
    }
}
