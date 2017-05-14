using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    /// <summary>
    /// Base class for range-based setBlock operations. Overload OnSetBlocks to create your own modify operation.
    /// </summary>
    public abstract class ModifyOpRange: ModifyOp
    {
        protected Vector3Int min;
        protected Vector3Int max;

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
                ChunkBlocks neighborBlocks = null;

                if (blocks.NeedToHandleNeighbors(ref min))
                {
                    // Left side
                    if (blocks.NeedToHandleNeighbors(ref min))
                    {
                        neighborBlocks = blocks.HandleNeighbor(ref min, Direction.west);
                        if (neighborBlocks!=null)
                        {
                            Vector3Int from = new Vector3Int(Env.ChunkSize, min.y, min.z);
                            Vector3Int to = new Vector3Int(Env.ChunkSize, max.y, max.z);
                            OnSetBlocksRaw(neighborBlocks, ref from, ref to);
                        }
                    }
                    // Bottom side
                    if (blocks.NeedToHandleNeighbors(ref min))
                    {
                        neighborBlocks = blocks.HandleNeighbor(ref min, Direction.down);
                        if (neighborBlocks!=null)
                        {
                            Vector3Int from = new Vector3Int(min.x, Env.ChunkSize, min.z);
                            Vector3Int to = new Vector3Int(max.x, Env.ChunkSize, max.z);
                            OnSetBlocksRaw(neighborBlocks, ref from, ref to);
                        }
                    }
                    // Back side
                    if (blocks.NeedToHandleNeighbors(ref min))
                    {
                        neighborBlocks = blocks.HandleNeighbor(ref min, Direction.south);
                        if (neighborBlocks!=null)
                        {
                            Vector3Int from = new Vector3Int(min.x, min.y, Env.ChunkSize);
                            Vector3Int to = new Vector3Int(max.x, max.y, Env.ChunkSize);
                            OnSetBlocksRaw(neighborBlocks, ref from, ref to);
                        }
                    }
                }

                if (blocks.NeedToHandleNeighbors(ref max))
                {
                    // Right side
                    if (blocks.NeedToHandleNeighbors(ref max))
                    {
                        neighborBlocks = blocks.HandleNeighbor(ref max, Direction.east);
                        if (neighborBlocks!=null)
                        {
                            Vector3Int from = new Vector3Int(-1, min.y, min.z);
                            Vector3Int to = new Vector3Int(-1, max.y, max.z);
                            OnSetBlocksRaw(neighborBlocks, ref from, ref to);
                        }
                    }
                    // Upper side
                    if (blocks.NeedToHandleNeighbors(ref max))
                    {
                        neighborBlocks = blocks.HandleNeighbor(ref max, Direction.up);
                        if (neighborBlocks!=null)
                        {
                            Vector3Int from = new Vector3Int(min.x, -1, min.z);
                            Vector3Int to = new Vector3Int(max.x, -1, max.z);
                            OnSetBlocksRaw(neighborBlocks, ref from, ref to);
                        }
                    }
                    // Front side
                    if (blocks.NeedToHandleNeighbors(ref max))
                    {
                        neighborBlocks = blocks.HandleNeighbor(ref max, Direction.north);
                        if (neighborBlocks!=null)
                        {
                            Vector3Int from = new Vector3Int(min.x, min.y, -1);
                            Vector3Int to = new Vector3Int(max.x, max.y, -1);
                            OnSetBlocksRaw(neighborBlocks, ref from, ref to);
                        }
                    }
                }
            }
            else
            {
                blocks.HandleNeighbors(blockData, min);
            }
        }
    }
}
