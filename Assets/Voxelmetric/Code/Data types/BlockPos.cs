using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

namespace Voxelmetric.Code.Data_types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockPos : IEquatable<BlockPos>
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

        #region Comparison

        //Overriding GetHashCode and Equals gives us a faster way to
        //compare two positions and we have to do that a lot
        public override int GetHashCode()
        {
            unchecked
            {
                int hash = 47;
                hash = hash * 227 + x.GetHashCode();
                hash = hash * 227 + y.GetHashCode();
                hash = hash * 227 + z.GetHashCode();
                return hash * 227;
            }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BlockPos))
                return false;
            BlockPos other = (BlockPos)obj;
            return Equals(other);
        }

        public bool Equals(BlockPos other)
        {
            if (GetHashCode() != other.GetHashCode())
                return false;
            if (x != other.x)
                return false;
            if (y != other.y)
                return false;
            if (z != other.z)
                return false;
            return true;
        }
        
        public static bool operator ==(BlockPos pos1, BlockPos pos2)
        {
            return pos1.Equals(pos2);
        }

        public static bool operator !=(BlockPos pos1, BlockPos pos2)
        {
            return !pos1.Equals(pos2);
        }

        #endregion

        //You can safely use BlockPos as part of a string like this:
        //"block at " + BlockPos + " is broken."
        public override string ToString()
        {
            return "(" + x + ", " + y + ", " + z + ")";
        }

        #region Byte conversion

        public byte[] ToBytes()
        {
            return new[] { x, y, z };
        }

        public static BlockPos FromBytes(byte[] bytes)
        {
            return new BlockPos(
                (byte)BitConverter.ToChar(bytes, 0),
                (byte)BitConverter.ToChar(bytes, 1),
                (byte)BitConverter.ToChar(bytes, 2)
                );
        }

        #endregion
    }
}