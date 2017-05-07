using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    public class SetBlockContext
    {
        //! Parent action
        public readonly ModifyBlockContext ParentContext;
        //! Block which is to be worked with
        public readonly BlockData Block;
        //! Starting block position within chunk
        public readonly int IndexFrom;
        //! Ending block position within chunk
        public readonly int IndexTo;
        //! If true we want to mark the block as modified
        public readonly bool SetBlockModified;

        public SetBlockContext(int index, BlockData block, bool setBlockModified, ModifyBlockContext parentContext = null)
        {
            ParentContext = parentContext;
            Block = block;
            IndexFrom = IndexTo = index;
            SetBlockModified = setBlockModified;
        }

        public SetBlockContext(int indexFrom, int indexTo, BlockData block, bool setBlockModified, ModifyBlockContext parentContext = null)
        {
            ParentContext = parentContext;
            Block = block;
            IndexFrom = indexFrom;
            IndexTo = indexTo;
            SetBlockModified = setBlockModified;
        }

        public bool IsRange()
        {
            return IndexFrom != IndexTo;
        }
    }
}