using System;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public struct SetBlockContext : IEquatable<SetBlockContext>
    {
        //! Block which is to be worked with
        public readonly BlockData Block;
        //! Starting block position within chunk
        public readonly int IndexFrom;
        //! Ending block position within chunk
        public readonly int IndexTo;
        //! If true we want to mark the block as modified
        public readonly bool SetBlockModified;

        public SetBlockContext(int index, BlockData block, bool setBlockModified)
        {
            Block = block;
            IndexFrom = IndexTo = index;
            SetBlockModified = setBlockModified;
        }

        public SetBlockContext(int indexFrom, int indexTo, BlockData block, bool setBlockModified)
        {
            Block = block;
            IndexFrom = indexFrom;
            IndexTo = indexTo;
            SetBlockModified = setBlockModified;
        }

        public bool IsRange()
        {
            return IndexFrom!=IndexTo;
        }

        public bool Equals(SetBlockContext other)
        {
            return IndexFrom==other.IndexFrom && IndexTo==other.IndexTo && Block.Equals(other.Block);
        }

        public static bool operator ==(SetBlockContext a, SetBlockContext b)
        {
            return a.IndexFrom==b.IndexFrom && a.IndexTo==b.IndexTo && a.Block.Equals(b.Block);
        }

        public static bool operator !=(SetBlockContext a, SetBlockContext b)
        {
            return a.IndexFrom!=b.IndexFrom || a.IndexTo!=b.IndexTo || a.Block.Equals(b.Block);
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is SetBlockContext && Equals((SetBlockContext)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = Block.Type.GetHashCode();
                hashCode = (hashCode * 397) ^ IndexFrom;
                hashCode = (hashCode * 397) ^ IndexTo;
                return hashCode;
            }
        }
    }
}