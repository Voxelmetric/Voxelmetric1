using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Runtime.Serialization.Formatters.Binary;
using System.Runtime.Serialization;
using System.IO;

[Serializable]
public class Save : ISerializable
{
    public BlockPos[] positions = new BlockPos[0];
    public Block[] blocks = new Block[0];

    public bool changed = false;

    [NonSerialized()] private Chunk chunk;

    private string[] names = new string[0];
    private ushort[] types = new ushort[0];

    private int version = -1;

    public Chunk Chunk { get { return chunk; } }

    public int Version { get { return version; } }

    public Save(Chunk chunk, Save existing) {
        this.chunk = chunk;

        Dictionary<BlockPos, Block> blocksDictionary = new Dictionary<BlockPos, Block>();

        if (existing != null) {
            //Because existing saved blocks aren't marked as modified we have to add the
            //blocks already in the save file if there is one.
            existing.AddBlocks(blocksDictionary);
        }

        // Then add modified blocks from this chunk
        foreach (var pos in chunk.blocks.modifiedBlocks) {
            //remove any existing blocks in the dictionary as they're
            //from the existing save and are overwritten
            blocksDictionary.Remove(pos);
            blocksDictionary.Add(pos, chunk.blocks.Get(pos));
            changed = true;
        }

        blocks = new Block[blocksDictionary.Keys.Count];
        positions = new BlockPos[blocksDictionary.Keys.Count];

        Dictionary<string, ushort> blockTypes = new Dictionary<string, ushort>();

        int index = 0;
        foreach (var pair in blocksDictionary) {
            blocks[index] = pair.Value;
            positions[index] = pair.Key;
            index++;
            blockTypes[pair.Value.name] = pair.Value.Type;
        }

        names = new string[blockTypes.Keys.Count];
        types = new ushort[blockTypes.Keys.Count];

        index = 0;
        foreach (var pair in blockTypes) {
            names[index] = pair.Key;
            types[index] = pair.Value;
            index++;
        }
    }

    public Dictionary<ushort, string> MakeTypeMap() {
        var typeMap = new Dictionary<ushort, string>();
        for (int idx = 0; idx < names.Length; ++idx) {
            typeMap[types[idx]] = names[idx];
        }
        return typeMap;
    }

    private void AddBlocks(Dictionary<BlockPos, Block> blocksDictionary) {
        for (int i = 0; i < blocks.Length; i++) {
            blocksDictionary.Add(positions[i], blocks[i]);
        }
    }

    // Constructor only used for deserialization
    protected Save(SerializationInfo info, StreamingContext context) {
        if (Utils.HasValue(info, "version"))
            version = info.GetInt32("version");
        else
            version = 0;  // previously version didn't exist
        changed = info.GetBoolean("changed");
        positions = info.GetValue("positions", typeof(BlockPos[])) as BlockPos[];
        blocks = info.GetValue("blocks", typeof(Block[])) as Block[];
        if (version >= 1) {
            names = info.GetValue("names", typeof(string[])) as string[];
            types = info.GetValue("types", typeof(ushort[])) as ushort[];
        }
    }
    // Gets information for serialization
    public virtual void GetObjectData(SerializationInfo info, StreamingContext context) {
        version = 1;
        info.AddValue("version", version);
        info.AddValue("changed", changed);
        info.AddValue("positions", positions);
        info.AddValue("blocks", blocks);
        info.AddValue("names", names);
        info.AddValue("types", types);
    }
}