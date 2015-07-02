using UnityEngine;
using System.Collections.Generic;

public class BlockIndex {

    public BlockIndex(){
        AddBlockType(new BlockAir());
    }

    public List<BlockController> controllers = new List<BlockController>();
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

        controllers.Add(controller);
        names.Add(controller.Name().ToLower().Replace(" ", ""), index);
        return index;
    }

    public void GetMissingDefinitions() {
        textureIndex = new TextureIndex();

        LoadMeshes.GetAndLoadMeshBlocks();

        BlockDefinition[] definitions = World.instance.gameObject.GetComponentsInChildren<BlockDefinition>();

        foreach (var def in definitions)
        {
            if(def.enabled)
                def.AddToBlocks();
        }
    }

}
