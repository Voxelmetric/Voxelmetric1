using UnityEngine;
using System.Collections.Generic;
using System;

public class BlockIndex {

    public BlockIndex(string blockFolder, World world){

        AddBlockType(new BlockAir());
        AddBlockType(new BlockSolid());

        GetMissingDefinitions(world, blockFolder);
    }

    public List<BlockController> controllers = new List<BlockController>();
    public Dictionary<string, int> names = new Dictionary<string, int>();

    /// <summary>
    /// Adds a block type to the index and adds it's name to a dictionary for quick lookup
    /// </summary>
    /// <param name="controller">The controller object for this block</param>
    /// <returns>The index of the block</returns>
    public int AddBlockType(BlockController controller)
    {
        int index = controllers.Count;

        if (index == ushort.MaxValue)
        {
            Debug.LogError("Too many block types!");
            return -1;
        }

        if (names.ContainsKey(controller.Name(0)))
        {
            Debug.LogError("Two blocks with the name " + controller.Name(0) + " are defined");
            return -1;
        } 

        controllers.Add(controller);

        names.Add(controller.Name(0).ToLower().Replace(" ", ""), index);
        return index;
    }

    //World is only needed for setting up the textures
    void GetMissingDefinitions(World world, string blockFolder) {
        ConfigLoader<BlockConfig> config = new ConfigLoader<BlockConfig>(new string[] { blockFolder});
        foreach (var blockConfig in config.AllConfigs())
        {
            var type = Type.GetType(blockConfig.controller + ", " + typeof(BlockController).Assembly, false);
            if (type == null)
                Debug.LogError("Could not create controller " + blockConfig.controller);

            BlockController controller = (BlockController)Activator.CreateInstance(type);
            controller.SetUpController(blockConfig, world);
            AddBlockType(controller);
        }
    }
}
