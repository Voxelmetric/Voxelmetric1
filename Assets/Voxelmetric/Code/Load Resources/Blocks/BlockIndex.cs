using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Newtonsoft.Json;

public class BlockIndex {

    public Dictionary<int, BlockConfig> configs = new Dictionary<int, BlockConfig>();
    public Dictionary<string, int> names = new Dictionary<string, int>();
    int largestIndexSoFar = 0;

    public BlockIndex(string blockFolder, World world){

        // Add the static solid void block type
        AddBlockType(new BlockConfig()
        {
            name = "void",
            type = 0,
            world = world,
            blockClass = typeof(Block),
            solid = true,
            canBeWalkedOn = false,
            transparent = false,
            canBeWalkedThrough = false,
        });

        // Add the static air block type
        AddBlockType(new BlockConfig() {
            name = "air",
            type = 1,
            world = world,
            blockClass = typeof(Block),
            solid = false,
            canBeWalkedOn = false,
            transparent = true,
            canBeWalkedThrough = true,
        });

        // If you want to add block types with a different method
        // this is the place to insert them

        //Add all the block definitions defined in the config files
        GetBlocksFromConfigs(world, blockFolder);
    }

    /// <summary>
    /// Adds a block type to the index and adds it's name to a dictionary for quick lookup
    /// </summary>
    /// <param name="controller">The controller object for this block</param>
    /// <returns>The index of the block</returns>
    public void AddBlockType(BlockConfig config)
    {
        // Use the type defined in the config if there is one,
        // otherwise add one to the largest index so far.
        if (config.type == -1)
        {
            config.type = largestIndexSoFar + 1;
        }

        if (config.type == ushort.MaxValue)
        {
            Debug.LogError("Too many block types!");
        }

        if (names.ContainsKey(config.name))
        {
            Debug.LogError("Two blocks with the name " + config.name + " are defined");
        }

        configs.Add(config.type, config);
        names.Add(config.name, config.type);

        if (config.type > largestIndexSoFar)
        {
            largestIndexSoFar = config.type;
        }
    }

    //World is only needed for setting up the textures
    void GetBlocksFromConfigs(World world, string blockFolder) {

        var configFiles = UnityEngine.Resources.LoadAll<TextAsset>(blockFolder);

        foreach (var configFile in configFiles)
        {
            Hashtable configHash = JsonConvert.DeserializeObject<Hashtable>(configFile.text);

            Type configType = Type.GetType(configHash["configClass"] + ", " + typeof(BlockConfig).Assembly, false);

            if (configType == null)
                Debug.LogError("Could not create config for " + configHash["configClass"]);

            BlockConfig config = (BlockConfig)Activator.CreateInstance(configType);

            config.SetUp(configHash, world);

            AddBlockType(config);
        }
    }

    public int GetType(string name)
    {
        int type;
        if (names.TryGetValue(name, out type))
        {
            return type;
        }
        else
        {
            Debug.LogWarning("Block not found: " + name);
            return 0;
        }
    }

    public BlockConfig GetConfig(int index)
    {
        BlockConfig config;
        if (configs.TryGetValue(index, out config))
        {
            return config;
        }
        else
        {
            Debug.LogWarning("Config not found: " + index);
            return null;
        }
    }
}
