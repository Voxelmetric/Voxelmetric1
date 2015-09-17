using System;

[Serializable]
public struct BlockData
{
    // This can be set to a long if 32 bits isn't enough memory but be careful raising this too high
    public int data;

    public bool this[int index]
    {
        get
        {
            return ((data >> index) & 0x1) == 1;
        }

        set
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
    }

    public void SetData(int index, int value, int width)
    {
        for (int i = 0; i < width; i++)
        {
            this[index + i] = ((value >> i) & 0x1) == 1;
        }
    }

    public int GetData(int index, int width)
    {
        return (int)((data >> index) & (16 ^ width));
    }

}