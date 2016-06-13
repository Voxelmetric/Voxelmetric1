using System;
using System.Collections.Generic;
using System.Text;
using NUnit.Framework;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

/// <summary>
/// Testing utilities
/// </summary>
public class TestUtils {

    /// <summary>
    /// Create a default world object for testing
    /// </summary>
    /// <returns></returns>
    public static World CreateWorldDefault() {
        World world = CreateWorldObject();
        world.worldConfig = "default";
        world.worldName = "defaultTest";
        return world;
    }

    /// <summary>
    /// Create a colored world object for testing
    /// </summary>
    /// <returns></returns>
    public static World CreateWorldColored() {
        World world = CreateWorldObject();
        world.worldConfig = "colored";
        world.worldName = "coloredTest";
        return world;
    }

    private static World CreateWorldObject() {
        var gameObject = new GameObject();
        return gameObject.AddComponent<World>();
    }

    public class AutoDictionary<TKey, TValue> : Dictionary<TKey, TValue> {

        public new TValue this[TKey key] {
            get {
                TValue value;
                if (!TryGetValue(key, out value)) {
                    if (value == null)
                        value = Activator.CreateInstance<TValue>();
                    base[key] = value;
                }
                return value;
            }
            set {
                base[key] = value;
            }
        }
    }

    public static void DebugBlockCounts(string name, Dictionary<string, int> blockCounts) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(name + " block counts");
        foreach (var pair in blockCounts) {
            sb.AppendLine(pair.Key + "=" + pair.Value);
        }
        Debug.Log(sb.ToString());
    }

    public static void DebugChunks(string name, ICollection<Chunk> chunks) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(name + " chunks");
        foreach (var chunk in chunks) {
            sb.AppendLine(chunk.ToString());
        }
        Debug.Log(sb.ToString());
    }

    public static readonly BlockPosEnumerable LocalPosns = new BlockPosEnumerable(BlockPos.one * Env.ChunkSize);

    public static AutoDictionary<string, int> FindWorldBlockCounts(World world) {
        var worldBlockCounts = new AutoDictionary<string, int>();
        foreach (var chunk in world.chunks.chunkCollection) {
            var chunkBlockCounts = FindChunkBlockCounts(chunk);
            foreach (var pair in chunkBlockCounts) {
                worldBlockCounts[pair.Key] += pair.Value;
            }
        }
        return worldBlockCounts;
    }

    public static AutoDictionary<string, int> FindChunkBlockCounts(Chunk chunk) {
        AutoDictionary<string, int> chunkBlockCounts = new AutoDictionary<string, int>();
        foreach (BlockPos localPos in LocalPosns) {
            Block block = chunk.blocks.GetBlock(localPos);
            chunkBlockCounts[block.name]++;
        }
        return chunkBlockCounts;
    }

    public static void SetChunkBlocksRandom(Chunk chunk, System.Random rand) {
        World world = chunk.world;

        const ushort blockTypes = 2;
        Block[] rndBlocks = new Block[blockTypes];

        for (ushort type = 0; type < blockTypes; ++type)
        {
            var config = world.blockProvider.GetConfig(type);
            Assert.IsNotNull(config, "config");
            Assert.IsNotNull(config.blockClass, "config.blockClass");

            Block block = new Block(type, config);
            rndBlocks[type] = block;
        }

        foreach (BlockPos localPos in LocalPosns)
            chunk.blocks.Set(localPos, new BlockData(rndBlocks[rand.Next(2)].type));
    }

    public static void SetChunkBlocks(Chunk chunk, ushort type) {
        World world = chunk.world;

        var config = world.blockProvider.GetConfig(type);
        Assert.IsNotNull(config, "config");
        Assert.IsNotNull(config.blockClass, "config.blockClass");

        Block block = new Block(type, config);

        foreach (BlockPos localPos in LocalPosns)
            chunk.blocks.Set(localPos, new BlockData(block.type));
    }

    public static void AssertEqualContents(Chunk expected, Chunk actual, string message) {
        foreach (BlockPos localPos in LocalPosns) {
            Block expBlock = expected.blocks.GetBlock(localPos);
            Block actBlock = actual.blocks.GetBlock(localPos);
            Assert.AreEqual(expBlock.type, actBlock.type, message + " type at " + localPos);
        }
    }

}
