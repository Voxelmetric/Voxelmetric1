using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    public sealed class ModifyOpCuboid: ModifyOpRange
    {
        /// <summary>
        /// Performs a ranged set operation of cuboid shape
        /// </summary>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="min">Starting positon in local chunk coordinates</param>
        /// <param name="max">Ending position in local chunk coordinates</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="parentContext">Context of a parent which performed this operation</param>
        public ModifyOpCuboid(BlockData blockData, Vector3Int min, Vector3Int max, bool setBlockModified,
            ModifyBlockContext parentContext = null): base(blockData, min, max, setBlockModified, parentContext)
        {
        }

        protected override void OnSetBlocks(ChunkBlocks blocks)
        {
            for (int y = min.y; y<=max.y; ++y)
            {
                for (int z = min.z; z<=max.z; ++z)
                {
                    int index = Helpers.GetChunkIndex1DFrom3D(min.x, y, z);
                    for (int x = min.x; x<=max.x; ++x, ++index)
                    {
                        blocks.ProcessSetBlock(blockData, index, setBlockModified);
                    }
                }
            }
        }
    }
}
