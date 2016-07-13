using System;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public struct SetBlockContext : IEquatable<SetBlockContext>
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

        public bool Equals(SetBlockContext other)
        {
            return Index==other.Index && Block.Equals(other.Block);
        }

        public static bool operator ==(SetBlockContext a, SetBlockContext b)
        {
            return a.Index==b.Index && a.Block.Equals(b.Block);
        }

        public static bool operator !=(SetBlockContext a, SetBlockContext b)
        {
            return a.Index!=b.Index || a.Block.Equals(b.Block);
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
                return (Block.GetHashCode()*397)^Index;
            }
        }
    }
}