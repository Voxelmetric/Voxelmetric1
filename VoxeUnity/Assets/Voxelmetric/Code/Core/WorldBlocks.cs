using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public class WorldBlocks
    {
        World world;

        public WorldBlocks(World world)
        {
            this.world = world;
        }

        /// <summary>
        /// Gets the chunk and retrives the block data at the given coordinates
        /// </summary>
        /// <param name="pos">Global position of the block data</param>
        public BlockData Get(Vector3Int pos)
        {
            Chunk chunk = world.chunks.Get(pos);
            if (chunk != null && pos.y>=world.config.minY)
            {
                Vector3Int vector3Int = new Vector3Int(
                    pos.x & Env.ChunkMask,
                    pos.y & Env.ChunkMask,
                    pos.z & Env.ChunkMask
                    );

                return chunk.blocks.Get(vector3Int);
            }

            return new BlockData(BlockProvider.AirType);
        }

        /// <summary>
        /// Gets the chunk and retrives the block at the given coordinates
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        public Block GetBlock(Vector3Int pos)
        {
            Chunk chunk = world.chunks.Get(pos);
            if (chunk != null && pos.y>=world.config.minY)
            {
                Vector3Int vector3Int = new Vector3Int(
                    pos.x & Env.ChunkMask,
                    pos.y & Env.ChunkMask,
                    pos.z & Env.ChunkMask
                    );

                BlockData blockData = chunk.blocks.Get(vector3Int);
                return world.blockProvider.BlockTypes[blockData.Type];
            }

            return world.blockProvider.BlockTypes[BlockProvider.AirType];
        }

        /// <summary>
        /// Gets the chunk and sets the block data at the given coordinates
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void Set(Vector3Int pos, BlockData blockData)
        {
            Chunk chunk = world.chunks.Get(pos);
            if (chunk==null)
                return;

            Vector3Int vector3Int = new Vector3Int(
                pos.x & Env.ChunkMask,
                pos.y & Env.ChunkMask,
                pos.z & Env.ChunkMask
                );

            chunk.blocks.Set(vector3Int, blockData);
        }

        /// <summary>
        /// Gets the chunk and sets the block data at the given coordinates, updates the chunk and its
        /// neighbors if the Update chunk flag is true or not set.
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">The block be placed</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        public void Modify(Vector3Int pos, BlockData blockData, bool setBlockModified)
        {
            Chunk chunk = world.chunks.Get(pos);
            if (chunk==null)
                return;

            Vector3Int vector3Int = new Vector3Int(
                pos.x & Env.ChunkMask,
                pos.y & Env.ChunkMask,
                pos.z & Env.ChunkMask
                );

            chunk.blocks.Modify(vector3Int, blockData, setBlockModified);
        }
    }
}
