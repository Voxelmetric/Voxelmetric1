using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    /// <summary>
    /// Base class for range-based setBlock operations. Overload OnSetBlocks to create your own modify operation.
    /// </summary>
    public abstract class ModifyOpRange: ModifyOp
    {
        protected readonly Vector3Int min;
        protected readonly Vector3Int max;

        protected ModifyOpRange(BlockData blockData, Vector3Int min, Vector3Int max, bool setBlockModified,
            ModifyBlockContext parentContext = null): base(blockData, setBlockModified, parentContext)
        {
            this.min = min;
            this.max = max;
        }

        protected override bool IsRanged()
        {
            return min!=max;
        }

        protected override void OnPostSetBlocks(ChunkBlocks blocks)
        {
            if (parentContext!=null)
                parentContext.ChildActionFinished();

            if (IsRanged())
            {
                // Right side
                Vector3Int pos = new Vector3Int(max.x, (min.y+max.y)>>1, (min.z+max.z)>>1);
                if (blocks.NeedToHandleNeighbors(ref pos))
                    blocks.HandleNeighbor(blockData, ref pos, Direction.east);
                // Left side
                pos = new Vector3Int(min.x, (min.y+max.y)>>1, (min.z+max.z)>>1);
                if (blocks.NeedToHandleNeighbors(ref pos))
                    blocks.HandleNeighbor(blockData, ref pos, Direction.west);
                // Upper side
                pos = new Vector3Int((min.x+max.x)>>1, max.y, (min.z+max.z)>>1);
                if (blocks.NeedToHandleNeighbors(ref pos))
                    blocks.HandleNeighbor(blockData, ref pos, Direction.up);
                // Bottom side
                pos = new Vector3Int((min.x+max.x)>>1, min.y, (min.z+max.z)>>1);
                if (blocks.NeedToHandleNeighbors(ref pos))
                    blocks.HandleNeighbor(blockData, ref pos, Direction.down);
                // Front side
                pos = new Vector3Int((min.x+max.x)>>1, (min.y+max.y)>>1, min.z);
                if (blocks.NeedToHandleNeighbors(ref pos))
                    blocks.HandleNeighbor(blockData, ref pos, Direction.north);
                // Back side
                pos = new Vector3Int((min.x+max.x)>>1, (min.y+max.y)>>1, max.z);
                if (blocks.NeedToHandleNeighbors(ref pos))
                    blocks.HandleNeighbor(blockData, ref pos, Direction.south);
            }
            else
            {
                blocks.HandleNeighbors(blockData, min);
            }
        }
    }
}
