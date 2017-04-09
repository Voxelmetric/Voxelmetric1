using System;

namespace Voxelmetric.Code.Data_types
{
    /*
     * A compressed representation of AABB with integer coordinates.
     * For each range there are 8 bits reserved. Therefore, each axis can contain values from 0..127
     * Block type data is 16 bits long.
     * 
     * Mask:
     * x1 = 0..7
     * y1 = 8..15
     * z1 = 16..23
     * x2 = 24..31
     * y2 = 32..39
     * z2 = 40..47
     * block data = 48..63
     */

    public struct BlockDataAABB: IEquatable<BlockDataAABB>
    {
        public readonly long data;

        public int MinX
        {
            get { return (sbyte)(data&0xFF); }
        }

        public int MinY
        {
            get { return (sbyte)((data>>8)&0xFF); }
        }

        public int MinZ
        {
            get { return (sbyte)((data>>16)&0xFF); }
        }

        public int MaxX
        {
            get { return (sbyte)((data>>24)&0xFF); }
        }

        public int MaxY
        {
            get { return (sbyte)((data>>32)&0xFF); }
        }

        public int MaxZ
        {
            get { return (sbyte)((data>>40)&0xFF); }
        }

        public ushort Data
        {
            get { return (ushort)(data>>48); }
        }

        public BlockDataAABB(ushort type, int x1, int y1, int z1, int x2, int y2, int z2)
        {
            data = ((long)x1&0xFF)|
                   (((long)y1 &0xFF)<<8)|
                   (((long)z1 &0xFF)<<16)|
                   (((long)x2 &0xFF)<<24)|
                   (((long)y2 &0xFF)<<32)|
                   (((long)z2 &0xFF)<<40)|
                   ((long)type<<48);
        }

        public bool IsInside(int x, int y, int z)
        {
            return x>(int)(data&0xFF) && x<(int)((data&0xFF)<<24) &&
                   y>(int)((data&0xFF)<<8) && y<(int)((data&0xFF)<<32) &&
                   z>(int)((data&0xFF)<<16) && z<(int)((data&0xFF)<<40);

        }

        public bool IsInside(ref Vector3Int pos)
        {
            return pos.x>(int)(data&0xFF) && pos.x<(int)((data&0xFF)<<24) &&
                   pos.y>(int)((data&0xFF)<<8) && pos.y<(int)((data&0xFF)<<32) &&
                   pos.z>(int)((data&0xFF)<<16) && pos.z<(int)((data&0xFF)<<40);
        }

        #region Object comparison

        public bool Equals(BlockDataAABB other)
        {
            return data==other.data;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is BlockDataAABB && Equals((BlockDataAABB)obj);
        }

        public override int GetHashCode()
        {
            return data.GetHashCode();
        }

        public static bool operator==(BlockDataAABB data1, BlockDataAABB data2)
        {
            return data1.data==data2.data;
        }

        public static bool operator!=(BlockDataAABB data1, BlockDataAABB data2)
        {
            return data1.data!=data2.data;
        }

        #endregion
    }
}
