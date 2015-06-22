using System;
using UnityEngine;

[Serializable]
public struct Block
{
    public readonly ushort type;
    public byte data1;
    public byte data2;
    public byte data3;
    public byte data4;
    public bool modified;

    public Block(int type)
    {
        this.type = (ushort)type;
        modified = true;
        data1 = 0;
        data2 = 0;
        data3 = 0;
        data4 = 0;
    }

    //Mappings
    public static BlockIndex index = new BlockIndex();

    public BlockController controller
    {
        get {
            if (type >= index.controllers.Count)
            {
                Debug.LogError("Block " + type + " is out of range");
            }
            return index.controllers[type];
        }
    }

    public static implicit operator BlockController(Block block)
    {
        return index.controllers[block.type];
    }

    public override string ToString()
    {
        return index.controllers[type].Name();
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
        if (index.names.TryGetValue(s, out blockIndex))
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