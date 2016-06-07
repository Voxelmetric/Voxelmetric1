using System;
using System.Runtime.InteropServices;
using UnityEngine;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Data_types
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockData
    {
        int data;

        public byte this[int index]
        {
            get
            {
                switch (index)
                {
                    case 0:
                        return (byte)((data & 0x000000FF));
                    case 1:
                        return (byte)((data & 0x0000FF00) >> 8);
                    case 2:
                        return (byte)((data & 0x00FF0000) >> 16);
                    case 3:
                        return (byte)((data & 0xFF000000) >> 24);
                    default:
                        Debug.LogWarning("block data index out of range");
                        return 0;
                }
            }
            set
            {
                byte[] bytes = new byte[4];
                for (int i = 0; i < 4; i++) { bytes[i] = this[i]; }
                bytes[index] = value;

                data = bytes[0] | (bytes[1] << 8) | (bytes[2] << 16) | (bytes[3] << 24);
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
            if (Toggle.UseMultipleWorlds)
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
            if (Toggle.UseMultipleWorlds)
            {
                this[3] = (byte)index;
            }
        }

    }
}