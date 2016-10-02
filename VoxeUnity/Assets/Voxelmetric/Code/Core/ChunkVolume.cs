using System;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public class ChunkVolume
    {
        //! Static array holding an empty block data
        private static readonly BlockData[] blocksEmpty = Helpers.CreateArray1D<BlockData>(Env.ChunkVolume);

        //! Array of block data
        private BlockData[] blocks = blocksEmpty;
        //! Number of blocks which are not air (non-empty blocks)
        public int NonEmptyBlocks = 0;

        public void Clear()
        {
            NonEmptyBlocks = 0;
            if (blocks!=blocksEmpty)
                Array.Clear(blocks, 0, blocks.Length);
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <returns>The block at the position</returns>
        public BlockData Get(ref Vector3Int pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return blocks[index];
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="index">Index to internal block buffer</param>
        /// <returns>The block at the position</returns>
        public BlockData Get(int index)
        {
            return blocks[index];
        }

        /// <summary>
        /// Sets the block at the given position
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void Set(ref Vector3Int pos, BlockData blockData)
        {
            // Block array about to be emptied
            if (blockData.Type==BlockProvider.AirType && NonEmptyBlocks==1)
            {
                // Point our current array to global empty array
                blocks = blocksEmpty;
                NonEmptyBlocks = 0;
                return;
            }

            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);

            // Block array about to be created
            if (blockData.Type!=BlockProvider.AirType && NonEmptyBlocks == 0)
            {
                // Allocate a new array
                blocks = Helpers.CreateArray1D<BlockData>(Env.ChunkVolume);
                blocks[index] = blockData;
                NonEmptyBlocks = 1;
                return;
            }

            // Nothing for us to do if there was no change
            BlockData oldBlockData = blocks[index];
            if (oldBlockData.Type == blockData.Type)
                return;

            // Update non-empty block count
            if (blockData.Type == BlockProvider.AirType)
                --NonEmptyBlocks;
            else
                ++NonEmptyBlocks;

            blocks[index] = blockData;
        }

        /// <summary>
        /// Sets the block at the given position
        /// </summary>
        /// <param name="index">Index to internal block buffer</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void Set(int index, BlockData blockData)
        {
            // Block array about to be emptied
            if (blockData.Type == BlockProvider.AirType && NonEmptyBlocks == 1)
            {
                // Point our current array to global empty array
                blocks = blocksEmpty;
                NonEmptyBlocks = 0;
                return;
            }

            // Block array about to be created
            if (blockData.Type != BlockProvider.AirType && NonEmptyBlocks == 0)
            {
                // Allocate a new array
                blocks = Helpers.CreateArray1D<BlockData>(Env.ChunkVolume);
                blocks[index] = blockData;
                NonEmptyBlocks = 1;
                return;
            }

            // Nothing for us to do if there was no change
            BlockData oldBlockData = blocks[index];
            if (oldBlockData.Type == blockData.Type)
                return;

            // Update non-empty block count
            if (blockData.Type == BlockProvider.AirType)
                --NonEmptyBlocks;
            else
                ++NonEmptyBlocks;

            blocks[index] = blockData;
        }
    }
}
