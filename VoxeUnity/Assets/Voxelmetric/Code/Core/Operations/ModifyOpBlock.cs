using System;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    public sealed class ModifyOpBlock: ModifyOp
    {
        private readonly int index;

        /// <summary>
        /// Performs a ranged set operation of cuboid shape
        /// </summary>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="index">Index in local chunk data</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="parentContext">Context of a parent which performed this operation</param>
        public ModifyOpBlock(BlockData blockData, int index, bool setBlockModified,
            ModifyBlockContext parentContext = null): base(blockData, setBlockModified, parentContext)
        {
            this.index = index;
        }

        protected override void OnSetBlocks(ChunkBlocks blocks)
        {
            blocks.ProcessSetBlock(blockData, index, setBlockModified);
        }

        protected override void OnSetBlocksRaw(ChunkBlocks blocks, ref Vector3Int min, ref Vector3Int max)
        {
            throw new Exception("ModifyOpBlock::OnSetBlocksRaw should never get called!");
        }

        protected override void OnPostSetBlocks(ChunkBlocks blocks)
        {
            if (parentContext!=null)
                parentContext.ChildActionFinished();

            int x, y, z;
            Helpers.GetChunkIndex3DFrom1D(index, out x, out y, out z);
            blocks.chunk.HandleNeighbors(blockData, new Vector3Int(x, y, z));
        }

        protected override bool IsRanged()
        {
            return false;
        }
    }
}
