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

    public BlockController controller
    {
        get {
            if (type >= Voxelmetric.resources.blockIndex.controllers.Count)
            {
                Debug.LogError("Block " + type + " is out of range");
            }
            return Voxelmetric.resources.blockIndex.controllers[type];
        }
    }

    public static implicit operator BlockController(Block block)
    {
        return Voxelmetric.resources.blockIndex.controllers[block.type];
    }

    public override string ToString()
    {
        return Voxelmetric.resources.blockIndex.controllers[type].Name();
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

    public static implicit operator Block(string s)
    {
        int blockIndex = 0;
        if (Voxelmetric.resources.blockIndex.names.TryGetValue(s, out blockIndex))
        {
            return blockIndex;
        }

        Debug.LogWarning("Block not found: " + s);
        return 0;
    }

    //Reserved block types
    public static Block Void
    {
        get { return new Block(ushort.MaxValue); }
    }

    public static Block Air
    {
        get { return new Block(0); }
    }

}