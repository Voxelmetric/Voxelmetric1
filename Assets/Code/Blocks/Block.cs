using System;
using UnityEngine;

[Serializable]
public struct Block
{
    public readonly byte type;
    public byte data1;
    public byte data2;
    public byte data3;
    public byte data4;
    public bool modified;

    public Block(int type)
    {
        this.type = (byte)type;
        modified = true;
        data1 = 0;
        data2 = 0;
        data3 = 0;
        data4 = 0;
    }

    //Mappings
    public static string[] typeNames = new string[] { "air", "stone", "dirt", "grass", "log", "leaves", "sand" };
    public static BlockController[] controllers = new BlockController[] { new BlockAir(), new Stone(), new Dirt(), new Grass(), new Log(), new Leaves(), new Sand() };

    public BlockController controller
    {
        get {
            if (type > 6)
                Debug.Log(type);
            return controllers[type]; }
    }

    public static implicit operator BlockController(Block block)
    {
        return controllers[block.type];
    }

    public override string ToString()
    {
        return typeNames[type];
    }

    public static implicit operator byte(Block block)
    {
        return block.type;
    }

    public static implicit operator Block(int b)
    {
        return new Block((byte)b);
    }

    public static implicit operator int (Block block)
    {
        return (int)block.type;
    }

    public static implicit operator Block(byte b)
    {
        return new Block(b);
    }

    public static implicit operator Block(string s)
    {
        for (int i = 0; i < typeNames.Length; i++)
        {
            if (s.ToLower() == typeNames[i].ToLower())
            {
                return (byte)i;
            }
        }

        return Void;
    }

    //Reserved block types
    public static Block Void
    {
        get { return new Block(255); }
    }

    public static Block Air
    {
        get { return new Block(0); }
    }

    public static Block Stone
    {
        get { return new Block(1); }
    }

    public static Block Dirt
    {
        get { return new Block(2); }
    }

    public static Block Grass
    {
        get { return new Block(3); }
    }

    public static Block Log
    {
        get { return new Block(4); }
    }

    public static Block Leaves
    {
        get { return new Block(5); }
    }

    public static Block Sand
    {
        get { return new Block(6); }
    }
}