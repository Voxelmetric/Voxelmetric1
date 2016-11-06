using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Newtonsoft.Json;
using System.Text;

public class BlockIndex {

    private Dictionary<int, BlockConfig> configs = new Dictionary<int, BlockConfig>();
    private Dictionary<string, ushort> names = new Dictionary<string, ushort>();
    private int largestIndexSoFar = 0;

    public const int VoidType = 0;
    public const int AirType = 1;

    public ICollection<string> Names { get { return names.Keys; } }
    public ICollection<ushort> Types { get { return names.Values; } }
    public ICollection<BlockConfig> Configs { get { return configs.Values; } }

    public BlockIndex(string blockFolder, World world){

        // Add the static solid void block type
        AddBlockType(new BlockConfig()
        {
            name = "void",
            type = VoidType,
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
            type = AirType,
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

        if(configs.ContainsKey(config.type)) {
            var oldConfig = configs[config.type];
            ushort type;
            if(names.TryGetValue(config.name, out type)) {
                if(type == config.type)
                    Debug.Log("configs already contains " + config.type + ": " + oldConfig + ", and names has the same type of " + config);
                else
                    Debug.Log("configs already contains " + config.type + ": " + oldConfig + ", and names has type " + type + " instead of the type of " + config);
            } else {
                Debug.Log("configs already contains " + config.type + ": " + oldConfig + ", but names doesn't contain the name of " + config);
            }
        } else {
            configs.Add(config.type, config);
            names.Add(config.name, (ushort)config.type);
        }

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

    public ushort GetBlockType(string name)
    {
        ushort type;
        if (names.TryGetValue(name, out type))
        {
            return type;
        }
        else
        {
            Debug.LogWarning("Block not found: " + name);
            return VoidType;
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

    public void DebugLog() {
        StringBuilder sb = new StringBuilder();
        foreach (var kvp in names.OrderBy(kvp => kvp.Value)) {
            sb.Append(kvp.Value);
            sb.Append(": ");
            sb.Append(kvp.Key);
            sb.Append("\n");
        }
        Debug.Log("BlockIndex\n" + sb.ToString());
    }
}
