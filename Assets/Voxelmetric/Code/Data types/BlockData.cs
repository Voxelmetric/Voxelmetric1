//#define SINGLEWORLD
#define MULTIWORLDS256
//#define MULTIWORLDS65k
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

    // Use of multiworlds is defined at the top of this file, uncomment the preprocessor directive
    // for a single world, up to 256 worlds or up to 65k worlds

#if (SINGLEWORLD)
    static int world = 0;
#elif (MULTIWORLDS256)
    byte world;
#elif(MULTIWORLDS65K)
    ushort world;
#endif

    /// <summary>
    /// If your game is using multiple worlds this function will return the world index
    /// </summary>
    /// <returns></returns>
    public int World()
    {
        return (int)world;
    }

    /// <summary>
    /// Sets the world index if there is one
    /// </summary>
    public void SetWorld(int index)
    {
#if (MULTIWORLDS256)
        world = (byte) index;
#elif(MULTIWORLDS65K)
        world = (byte) index;
#endif
    }

}