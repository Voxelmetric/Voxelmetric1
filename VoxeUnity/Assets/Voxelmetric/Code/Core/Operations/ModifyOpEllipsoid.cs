using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Operations
{
    public sealed class ModifyOpEllipsoid: ModifyOpRange
    {
        private readonly Vector3Int offset;
        private readonly float a2inv;
        private readonly float b2inv;

        /// <summary>
        /// Performs a ranged set operation of ellipsoid shape
        /// </summary>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="min">Starting positon in local chunk coordinates</param>
        /// <param name="max">Ending position in local chunk coordinates</param>
        /// <param name="offset"></param>
        /// <param name="a2inv"></param>
        /// <param name="b2inv"></param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="parentContext">Context of a parent which performed this operation</param>
        public ModifyOpEllipsoid(BlockData blockData, Vector3Int min, Vector3Int max, Vector3Int offset, float a2inv,
            float b2inv, bool setBlockModified, ModifyBlockContext parentContext = null)
            : base(blockData, min, max, setBlockModified, parentContext)
        {
            this.offset = offset;
            this.a2inv = a2inv;
            this.b2inv = b2inv;
        }

        protected override void OnSetBlocks(ChunkBlocks blocks)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(min.x, min.y, min.z);
            int yOffset = Env.ChunkSizeWithPaddingPow2-(max.z-min.z+1) * Env.ChunkSizeWithPadding;
            int zOffset = Env.ChunkSizeWithPadding-(max.x-min.x+1);

            for (int y = min.y; y<=max.y; ++y, index+=yOffset)
            {
                for (int z = min.z; z<=max.z; ++z, index+=zOffset)
                {
                    for (int x = min.x; x<=max.x; ++x, ++index)
                    {
                        int xx = x+offset.x;
                        int yy = y+offset.y;
                        int zz = z+offset.z;

                        float _x = xx*xx*a2inv;
                        float _y = yy*yy*b2inv;
                        float _z = zz*zz*a2inv;

                        if (_x+_y+_z<=1.0f)
                            blocks.ProcessSetBlock(blockData, index, setBlockModified);
                    }
                }
            }
        }

        protected override void OnSetBlocksRaw(ChunkBlocks blocks, ref Vector3Int from, ref Vector3Int to)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(from.x, from.y, from.z);
            int yOffset = Env.ChunkSizeWithPaddingPow2-(to.z- from.z+1) * Env.ChunkSizeWithPadding;
            int zOffset = Env.ChunkSizeWithPadding-(to.x-from.x+1);

            for (int y = from.y; y <= to.y; ++y, index+=yOffset)
            {
                for (int z = from.z; z <= to.z; ++z, index+=zOffset)
                {
                    for (int x = from.x; x <= to.x; ++x, ++index)
                    {
                        int xx = x + offset.x;
                        int yy = y + offset.y;
                        int zz = z + offset.z;

                        float _x = xx * xx * a2inv;
                        float _y = yy * yy * b2inv;
                        float _z = zz * zz * a2inv;

                        if (_x + _y + _z <= 1.0f)
                            blocks.SetRaw(index, blockData);
                    }
                }
            }
        }
    }
}
