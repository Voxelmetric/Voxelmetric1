using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

namespace Voxelmetric.Code.Data_types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockPos: IEquatable<BlockPos>
    {
        public readonly byte x, y, z;

        public BlockPos(int x, int y, int z)
        {
            Assert.IsTrue(x>=0 && x<byte.MaxValue);
            Assert.IsTrue(y>=0 && y<byte.MaxValue);
            Assert.IsTrue(z>=0 && z<byte.MaxValue);
            this.x = (byte)x;
            this.y = (byte)y;
            this.z = (byte)z;
        }

        public BlockPos(byte x, byte y, byte z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        #region Struct comparison

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = x.GetHashCode();
                hashCode = (hashCode*397)^y.GetHashCode();
                hashCode = (hashCode*397)^z.GetHashCode();
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is BlockPos && Equals((BlockPos)obj);
        }

        public bool Equals(BlockPos other)
        {
            return x==other.x && y==other.y && z==other.z;
        }

        public static bool operator==(BlockPos pos1, BlockPos pos2)
        {
            return pos1.x==pos2.x && pos1.y==pos2.y && pos1.z==pos2.z;
        }

        public static bool operator!=(BlockPos pos1, BlockPos pos2)
        {
            return !(pos1==pos2);
        }

        #endregion

        //You can safely use BlockPos as part of a string like this:
        //"block at " + BlockPos + " is broken."
        public override string ToString()
        {
            return "("+x+", "+y+", "+z+")";
        }
    }
}