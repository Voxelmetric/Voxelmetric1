using System;
using System.Runtime.InteropServices;

[Serializable]
[StructLayout(LayoutKind.Sequential)]
public struct BlockData
{
    int data;

    public byte this[int index]
    {
        get
        {
            return BitConverter.GetBytes(data)[index];
        }
        set
        {
            byte[] bytes = BitConverter.GetBytes(data);
            bytes[index] = value;
            data = BitConverter.ToInt32(bytes, 0);
        }
    }

    public bool this[int byteIndex, int boolIndex]
    {
        get
        {
           return ((this[byteIndex] >> boolIndex) & 0x1) == 1;
        }
        set
        {
            int byteAtIndex = this[byteIndex];
            
            int mask = (1 << boolIndex);
            if (value)
            {
                byteAtIndex |= mask;
            }
            else
            {
                byteAtIndex &= ~mask;
            }

            this[byteIndex] = (byte)byteAtIndex;
        }
    }

    public static implicit operator int(BlockData bd)
    {
        return (int)bd.data;
    }

    public static implicit operator BlockData(int i)
    {
        BlockData newBD = new BlockData();
        newBD.data = i;
        return newBD;
    }

    public bool GetBit(int index, bool value)
    {
        return ((data >> index) & 0x1) == 1;
    }

    public void SetBit(int index, bool value)
    {
        int mask = (1 << index);
        if (value)
        {
            data |= mask;
        }
        else
        {
            data &= ~mask;
        }
    }

    public int World()
    {
        if (Config.Toggle.UseMultipleWorlds)
        {
            return this[3];
        }
        else
        {
            return 0;
        }
    }

    public void SetWorld(int index)
    {
        if (Config.Toggle.UseMultipleWorlds)
        {
            this[3] = (byte)index;
        }
    }

}