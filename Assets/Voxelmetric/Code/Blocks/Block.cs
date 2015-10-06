using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public struct Block
{
    public readonly ushort type;
    
    public BlockData data;

    public Block(int type)
    {
        this.type = (ushort)type;
        data = new BlockData();
    }

    public Block(string name, World world)
    {
        int type;
        if (world.blockIndex.names.TryGetValue(name, out type))
        {
            this.type = (ushort)type;
            data = new BlockData();
        }
        else
        {
            this.type = 0;
            data = new BlockData();
            Debug.LogWarning("Block not found: " + name);
            Debug.LogWarning("Searched " + world.worldName + " with " + world.blockIndex.controllers.Count + " blocks");
        }
    }

    public BlockController controller
    {
        get {
            if (type >= world.blockIndex.controllers.Count)
            {
                Debug.LogError("Block " + type + " is out of range");
            }
            return world.blockIndex.controllers[type];
        }
    }

    public World world
    {
        get
        {
            return Voxelmetric.resources.worlds[data.World()];
        }
    }

    public BlockOverride blockOveride
    {
        get
        {
           return world.blockIndex.blockOverrides[type];
        }
    }

    public static implicit operator BlockController(Block block)
    {
        return block.world.blockIndex.controllers[block.type];
    }

    public override string ToString()
    {
        return world.blockIndex.controllers[type].Name();
    }

    public static implicit operator ushort(Block block)
    {
        return block.type;
    }

    public static implicit operator Block(int b)
    {
        return new Block((ushort)b);
    }

    public static implicit operator int (Block block)
    {
        return block.type;
    }

    public static implicit operator Block(ushort b)
    {
        return new Block(b);
    }

    //public static implicit operator Block(string s)
    //{
    //    int blockIndex = 0;
    //    if (Voxelmetric.resources.worlds[0].blockIndex.names.TryGetValue(s, out blockIndex))
    //    {
    //        return blockIndex;
    //    }

    //    Debug.LogWarning("Block not found: " + s);
    //    return 0;
    //}

    //Reserved block types
    public static Block Air
    {
        get { return new Block(0); }
    }

}