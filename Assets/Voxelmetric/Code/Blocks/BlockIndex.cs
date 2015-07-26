using UnityEngine;
using System.Collections.Generic;
using System;

public class BlockIndex {

    public BlockIndex(){
        AddBlockType(new BlockAir());
        AddBlockType(new BlockSolid());
    }

    public List<BlockController> controllers = new List<BlockController>();
    public List<BlockOverride> blockOverrides = new List<BlockOverride>();
    public Dictionary<string, int> names = new Dictionary<string, int>();

    public TextureIndex textureIndex;

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

        if (names.ContainsKey(controller.Name()))
        {
            Debug.LogError("Two blocks with the name " + controller.Name() + " are defined");
        } 

        controllers.Add(controller);
        BlockOverride blockOverride = GetBlockOverride(controller.Name());
        if(blockOverride != null)
            blockOverride.controller = controller;

        blockOverrides.Add(blockOverride);

        names.Add(controller.Name().ToLower().Replace(" ", ""), index);
        return index;
    }

    public void GetMissingDefinitions() {
        textureIndex = new TextureIndex();

        BlockDefinition[] definitions = World.instance.gameObject.GetComponentsInChildren<BlockDefinition>();

        foreach (var def in definitions)
        {
            if(def.enabled)
                def.AddToBlocks();
        }
    }

    BlockOverride GetBlockOverride(string blockName)
    {
        var type = Type.GetType(blockName + "Override" + ", " + typeof(BlockOverride).Assembly, false);
        if (type == null)
            return null;

        return (BlockOverride)Activator.CreateInstance(type);
    }

}
