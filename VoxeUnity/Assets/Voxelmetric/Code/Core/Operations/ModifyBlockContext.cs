using System;
using UnityEngine.Assertions;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    public class ModifyBlockContext
    {
        //! World this operation belong to
        private readonly World World;
        //! Action to perform once all child actions are finished
        private readonly Action<ModifyBlockContext> Action;
        //! Block which is to be worked with
        public readonly BlockData Block;
        //! Starting block position within chunk
        public readonly int IndexFrom;
        //! Ending block position within chunk
        public readonly int IndexTo;
        //! Parent action
        private int ChildActionsPending;
        //! If true we want to mark the block as modified
        public readonly bool SetBlockModified;

        public ModifyBlockContext(Action<ModifyBlockContext> action, World world, int index, BlockData block,
            bool setBlockModified)
        {
            World = world;
            Action = action;
            Block = block;
            IndexFrom = IndexTo = index;
            ChildActionsPending = 0;
            SetBlockModified = setBlockModified;
        }

        public ModifyBlockContext(Action<ModifyBlockContext> action, World world, int indexFrom, int indexTo,
            BlockData block, bool setBlockModified)
        {
            World = world;
            Action = action;
            Block = block;
            IndexFrom = indexFrom;
            IndexTo = indexTo;
            ChildActionsPending = 0;
            SetBlockModified = setBlockModified;
        }

        public void RegisterChildAction()
        {
            ++ChildActionsPending;
        }

        public void ChildActionFinished()
        {
            // Once all child actions are performed register this action in the world
            --ChildActionsPending;
            Assert.IsTrue(ChildActionsPending>=0);
            if (ChildActionsPending==0)
                World.RegisterModifyRange(this);
        }

        public void PerformAction()
        {
            Action(this);
        }
    }
}
