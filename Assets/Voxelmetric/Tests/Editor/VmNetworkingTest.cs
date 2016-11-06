using UnityEngine;
using UnityEditor;
using NUnit.Framework;
using System.Net;
using System.Threading;
using System.Collections.Generic;
using System;
using TransmitChunkData = VmNetworking.TransmitChunkData;

public class VmNetworkingTest {

    [Test]
    public void SerializationTest()
    {
        bool debug = false;

        World world = TestUtils.CreateWorldDefault();
        world.Configure();

        BlockPos chunkPos = new BlockPos(0, 0, 0);
        Chunk fromChunk = new Chunk(world, chunkPos);
        TestUtils.SetChunkBlocksRandom(fromChunk, new System.Random(444));
        
        if (debug) TestUtils.DebugBlockCounts("fromChunk", TestUtils.FindChunkBlockCounts(fromChunk));

        byte[] chunkData = fromChunk.blocks.ToBytes();
        Chunk toChunk = new Chunk(world, chunkPos);

        int chunkDataIndex = 0;
        while (chunkDataIndex < chunkData.Length) {
            byte[] message = new byte[VmNetworking.bufferLength];
            /*message[0] = TransmitChunkData.ID;
            int size = Math.Min(VmNetworking.bufferLength, TransmitChunkData.HeaderSize + chunkData.Length);
            BitConverter.GetBytes(size).CopyTo(message, TransmitChunkData.IdxSize);
            chunkPos.ToBytes().CopyTo(message, TransmitChunkData.IdxChunkPos);*/
            BitConverter.GetBytes(chunkDataIndex).CopyTo(message, TransmitChunkData.IdxDataOffset);
            BitConverter.GetBytes(chunkData.Length).CopyTo(message, TransmitChunkData.IdxDataLength);

            for (int i = TransmitChunkData.IdxData; i < message.Length; i++) {
                message[i] = chunkData[chunkDataIndex];
                chunkDataIndex++;

                if (chunkDataIndex >= chunkData.Length) {
                    break;
                }
            }

            toChunk.blocks.ReceiveChunkData(message);
        }

        // Check that toChunk has the same blocks as fromChunk
        if (debug) TestUtils.DebugBlockCounts("toChunk", TestUtils.FindChunkBlockCounts(toChunk));
        TestUtils.AssertEqualContents(fromChunk, toChunk, "serialized chunk");
    }

    private class TestMessageHandler : VmSocketState.IMessageHandler {

        private Chunk chunk;

        public TestMessageHandler(Chunk chunk) {
            this.chunk = chunk;
        }

        public int GetExpectedSize(byte messageType) {
            Assert.AreEqual(TransmitChunkData.ID, messageType, "messageType");
            return -TransmitChunkData.IdxSize;
        }

        public void HandleMessage(byte[] message) {
            chunk.blocks.ReceiveChunkData(message);
        }
    }

    [Test]
    public void VmSocketStateTest() {
        bool debug = false;

        World world = TestUtils.CreateWorldDefault();
        world.Configure();

        BlockPos chunkPos = new BlockPos(0, 0, 0);
        Chunk fromChunk = new Chunk(world, chunkPos);
        TestUtils.SetChunkBlocksRandom(fromChunk, new System.Random(444));

        if (debug) TestUtils.DebugBlockCounts("fromChunk", TestUtils.FindChunkBlockCounts(fromChunk));

        byte[] chunkData = fromChunk.blocks.ToBytes();
        Chunk toChunk = new Chunk(world, chunkPos);
        var messageHandler = new TestMessageHandler(toChunk);
        VmSocketState socketState = null;

        var rand = new System.Random(444);

        // NOTE: Mostly Copy/pasted from VmServer.SendChunk
        int chunkDataIndex = 0;
        while (chunkDataIndex < chunkData.Length) {
            int remaining = chunkData.Length - chunkDataIndex;
            //int size = VmNetworking.bufferLength;
            int size = Math.Min(VmNetworking.bufferLength, TransmitChunkData.HeaderSize + remaining);
            byte[] message = new byte[size];
            message[0] = TransmitChunkData.ID;
            BitConverter.GetBytes(size).CopyTo(message, TransmitChunkData.IdxSize);
            chunkPos.ToBytes().CopyTo(message, TransmitChunkData.IdxChunkPos);
            BitConverter.GetBytes(chunkDataIndex).CopyTo(message, TransmitChunkData.IdxDataOffset);
            BitConverter.GetBytes(chunkData.Length).CopyTo(message, TransmitChunkData.IdxDataLength);

            int idx = TransmitChunkData.IdxData;
            for (; idx < message.Length; idx++) {
                message[idx] = chunkData[chunkDataIndex];
                chunkDataIndex++;

                if (chunkDataIndex >= chunkData.Length) {
                    break;
                }
            }

            int messageLength = message.Length;
            if (idx < messageLength) {
                messageLength = idx + 1;
                if (debug) Debug.Log("messageLength=" + messageLength);
                Assert.AreEqual(size, messageLength, "messageLength");
                //BitConverter.GetBytes(messageLength).CopyTo(message, TransmitChunkData.IdxSize);
            }

            // Now receive in random chunks in the same order
            int currentOffset = 0;
            while (currentOffset < messageLength) {
                if (socketState == null)
                    socketState = new VmSocketState(messageHandler);
                int nextOffset = rand.Next(currentOffset + 1, currentOffset + 1 + message.Length);
                if (nextOffset == currentOffset)
                    nextOffset = currentOffset + 1;
                if (nextOffset > messageLength)
                    nextOffset = messageLength;
                
                int received = nextOffset - currentOffset;
                Array.Copy(message, currentOffset, socketState.buffer, 0, received);

                socketState.Receive(received);
                currentOffset = nextOffset;
            }
            
        }

        // Check that toChunk has the same blocks as fromChunk
        if (debug) TestUtils.DebugBlockCounts("toChunk", TestUtils.FindChunkBlockCounts(toChunk));
        TestUtils.AssertEqualContents(fromChunk, toChunk, "serialized chunk");
    }

    [Test]
    public void TCPTest() {
        bool debug = false;

        var chunkSizePos = BlockPos.one * Config.Env.ChunkSize;
        BlockPosEnumerable chunkPosns = new BlockPosEnumerable(-chunkSizePos, chunkSizePos, chunkSizePos, true);

        VmServer server = null;
        VmClient client = null;

        try {
            // Create two worlds, one as a server and another as a client to that server
            World serverWorld = TestUtils.CreateWorldDefault();
            serverWorld.Configure();
            server = new VmServer(serverWorld);

            Assert.AreEqual(0, server.ClientCount, "Initial client count");

            World clientWorld = TestUtils.CreateWorldDefault();
            clientWorld.Configure();
            client = new VmClient(clientWorld, server.ServerIP);

            // Wait a short while for the client and server to get done initializing
            Thread.Sleep(100); // 100 ms should be plenty of time

            Assert.AreEqual(1, server.ClientCount, "Connected client count");

            // Setup a chunk with random blocks on the server
            BlockPos chunkPos = new BlockPos(0, 0, 0);
            Chunk serverChunk = new Chunk(serverWorld, chunkPos);
            var rand = new System.Random(444);
            TestUtils.SetChunkBlocksRandom(serverChunk, rand);
            serverWorld.chunks.Set(chunkPos, serverChunk);

            // Setup air chunks on the client
            foreach (var pos in chunkPosns) {
                Chunk chunk = new Chunk(clientWorld, pos);
                TestUtils.SetChunkBlocks(chunk, Block.AirType);
                clientWorld.chunks.Set(pos, chunk);
            }

            // Have the client request the chunk
            client.RequestChunk(chunkPos);

            // Wait a short while for the request to be serviced
            Thread.Sleep(100); // 100 ms should be plenty of time

            // Test that the client chunk has been populated with the server data
            Chunk clientChunk = clientWorld.chunks.Get(chunkPos);
            TestUtils.AssertEqualContents(serverChunk, clientChunk, "clientChunk");

            // Request 26 chunks around the first all at once
            foreach (var pos in chunkPosns) {
                if (pos == chunkPos)
                    continue;
                if (debug) TestUtils.DebugBlockCounts("before chunk " + pos, TestUtils.FindChunkBlockCounts(clientWorld.chunks.Get(pos)));
                client.RequestChunk(pos);
            }

            // Wait a short while for the requests to be serviced
            Thread.Sleep(2000); // 2 s should be plenty of time -- I thought 1s would be but it wasn't sometimes, and nor is 2s!

            // Expect all client chunks to have been set to the void that the server has
            foreach (var pos in chunkPosns) {
                if (pos == chunkPos)
                    continue;
                Chunk chunk = clientWorld.chunks.Get(pos);
                if (debug) TestUtils.DebugBlockCounts("after chunk " + pos, TestUtils.FindChunkBlockCounts(chunk));
                var chunkBlockCounts = TestUtils.FindChunkBlockCounts(chunk);
                Assert.AreEqual(1, chunkBlockCounts.Count);
                Assert.IsTrue(chunkBlockCounts.ContainsKey("void"), "expected void for " + pos);
            }

        } finally {
            client.Disconnect();
            server.Disconnect();
        }
    }

    [Test]
    public void ThreadingTest() {
        bool debug = false;

        List<VmNetworking> networkings = new List<VmNetworking>();
        try {
            //Setup server
            VmNetworking serverNetworking = new VmNetworking();
            networkings.Add(serverNetworking);
            serverNetworking.isServer = true;
            serverNetworking.allowConnections = true;
            if (debug) Debug.Log("Starting Server (" + Thread.CurrentThread.ManagedThreadId + "): ");
            World serverWorld = TestUtils.CreateWorldDefault();
            serverNetworking.StartConnections(serverWorld);

            IPAddress serverIP = serverNetworking.server.ServerIP;
            Assert.IsNotNull(serverIP, "serverIP");

            // Start the client and connect to the server
            VmNetworking clientNetworking = new VmNetworking();
            networkings.Add(clientNetworking);
            clientNetworking.isServer = false;
            clientNetworking.serverIP = serverIP;
            if (debug) Debug.Log("Starting Client (" + Thread.CurrentThread.ManagedThreadId + "): ");
            World clientWorld = TestUtils.CreateWorldDefault();
            clientNetworking.StartConnections(clientWorld);

            // Wait a short while for the client and server to get done initializing
            Thread.Sleep(100); // 100 ms should be plenty of time

            // Have the client request a chunk
            BlockPos chunkPos = new BlockPos(0, 0, 0);
            clientNetworking.client.RequestChunk(chunkPos);

            // Wait a short while for the request to be serviced
            Thread.Sleep(100); // 100 ms should be plenty of time

            // Test that the client has the chunk requested
            clientWorld.chunks.Get(chunkPos);

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
