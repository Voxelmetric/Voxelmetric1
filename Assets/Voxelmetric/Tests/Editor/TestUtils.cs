using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using NUnit.Framework;
using UnityEngine;

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

    public static void DebugLog<TKey, TValue>(string name, Dictionary<TKey, TValue> dictionary) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(name);
        foreach(var pair in dictionary) {
            sb.Append("    " + pair.Key + "=");
            if ( pair.Value is IEnumerable )
                sb.AppendLine((pair.Value as IEnumerable).Cast<object>().Count().ToString());
            else
                sb.AppendLine(pair.Value.ToString());
        }
        Debug.Log(sb.ToString());
    }

    public static void DebugBlockCounts(string name, Dictionary<string, int> blockCounts) {
        DebugLog(name + " block counts", blockCounts);
    }

    public static void DebugChunks(string name, ICollection<Chunk> chunks) {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine(name + " chunks");
        foreach (var chunk in chunks) {
            sb.AppendLine(chunk.ToString());
        }
        Debug.Log(sb.ToString());
    }

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
        foreach (BlockPos localPos in Chunk.LocalPosns) {
            Block block = chunk.blocks.LocalGet(localPos);
            chunkBlockCounts[block.name]++;
        }
        return chunkBlockCounts;
    }

    public static int CountModifiedBlocks(World world) {
        int numModified = 0;
        foreach (var chunk in world.chunks.chunkCollection)
            numModified += chunk.blocks.modifiedBlocks.Count;
        return numModified;
    }

    public static int CountModifiedChunks(World world) {
        int numModified = 0;
        foreach (var chunk in world.chunks.chunkCollection)
            numModified += chunk.blocks.modifiedBlocks.Count > 0 ? 1 : 0;
        return numModified;
    }

    public static int LoadAll(World world) {
        int numLoaded = 0;
        foreach (var chunk in world.chunks.chunkCollection) {
            Save save = Serialization.Read(chunk);
            if (save != null) {
                ++numLoaded;
            }
        }
        return numLoaded;
    }

    public static void SetChunkBlocksRandom(Chunk chunk, System.Random rand) {
        World world = chunk.world;

        const int blockTypes = 2;
        for (int type = 0; type < blockTypes; ++type) {
            var config = world.blockIndex.GetConfig(type);
            Assert.IsNotNull(config, "config");
            Assert.IsNotNull(config.blockClass, "config.blockClass");
        }

        Block[] rndBlocks = new Block[blockTypes];
        for (ushort type = 0; type < blockTypes; ++type) {
            Block block = Block.New(type, world);
            rndBlocks[type] = block;
        }

        foreach (BlockPos localPos in Chunk.LocalPosns)
            chunk.blocks.LocalSet(localPos, rndBlocks[rand.Next(2)]);
    }

    public static void SetChunkBlocks(Chunk chunk, ushort type) {
        World world = chunk.world;

        var config = world.blockIndex.GetConfig(type);
        Assert.IsNotNull(config, "config");
        Assert.IsNotNull(config.blockClass, "config.blockClass");

        Block block = Block.New(type, world);

        foreach (BlockPos localPos in Chunk.LocalPosns)
            chunk.blocks.LocalSet(localPos, block);
    }

    public static void AssertEqualContents(Chunk expected, Chunk actual, string message) {
        foreach (BlockPos localPos in Chunk.LocalPosns) {
            Block expBlock = expected.blocks.LocalGet(localPos);
            Block actBlock = actual.blocks.LocalGet(localPos);
            Assert.AreEqual(expBlock.Type, actBlock.Type, message + " type at " + localPos);
        }
    }

}
