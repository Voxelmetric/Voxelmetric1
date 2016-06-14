using System;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public struct SetBlockContext : IComparable<SetBlockContext>, IEquatable<SetBlockContext>
    {
        //! Block which is to be worked with
        public readonly BlockData Block;
        //! Block position within chunk
        public readonly int Index;
        //! If true we want to mark the block as modified
        public readonly bool SetBlockModified;

        public SetBlockContext(int index, BlockData block, bool setBlockModified)
        {
            Block = block;
            Index = index;
            SetBlockModified = setBlockModified;
        }

        private static bool AreEqual(ref SetBlockContext a, ref SetBlockContext b)
        {
            return a.Index == b.Index && a.Block.Equals(b.Block);
        }

        public static bool operator ==(SetBlockContext lhs, SetBlockContext rhs)
        {
            return AreEqual(ref lhs, ref rhs);
        }

        public static bool operator !=(SetBlockContext lhs, SetBlockContext rhs)
        {
            return !AreEqual(ref lhs, ref rhs);
        }

        public int CompareTo(SetBlockContext other)
        {
            return AreEqual(ref this, ref other) ? 0 : 1;
        }

        public override bool Equals(object other)
        {
            if (!(other is SetBlockContext))
                return false;

            SetBlockContext vec = (SetBlockContext)other;
            return AreEqual(ref this, ref vec);
        }

        public bool Equals(SetBlockContext other)
        {
            return AreEqual(ref this, ref other);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Block.GetHashCode();
                hashCode = (hashCode * 397) ^ Index;
                return hashCode;
            }
        }
    }
}