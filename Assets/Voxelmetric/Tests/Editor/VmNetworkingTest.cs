using NUnit.Framework;
using System.Threading;
using System;
using Voxelmetric.Code.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;
using Voxelmetric.Code.VM;

public class VmNetworkingTest {

    [Test]
    public void SerializationTest()
    {
        bool debug = false;

        World world = TestUtils.CreateWorldDefault();
        world.Configure();

        BlockPos chunkPos = new BlockPos(0, 0, 0);
        Chunk fromChunk = Chunk.Create(world, chunkPos);
        TestUtils.SetChunkBlocksRandom(fromChunk, new System.Random(444));
        
        if (debug) TestUtils.DebugBlockCounts("fromChunk", TestUtils.FindChunkBlockCounts(fromChunk));

        byte[] chunkData = fromChunk.blocks.ToBytes();
        Chunk toChunk = Chunk.Create(world, chunkPos);

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
        if (debug) TestUtils.DebugBlockCounts("toChunk", TestUtils.FindChunkBlockCounts(toChunk));
        TestUtils.AssertEqualContents(fromChunk, toChunk, "serialized chunk");
    }

    [Test]
    public void TCPTest() {
        bool debug = false;

        var chunkSizePos = BlockPos.one * Env.ChunkSize;
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
            Chunk serverChunk = Chunk.Create(serverWorld, chunkPos);
            var rand = new System.Random(444);
            TestUtils.SetChunkBlocksRandom(serverChunk, rand);
            serverWorld.chunks.Set(chunkPos, serverChunk);

            // Setup air chunks on the client
            foreach (var pos in chunkPosns) {
                Chunk chunk = Chunk.Create(clientWorld, pos);
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
            Thread.Sleep(1000); // 1 s should be plenty of time

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

}
