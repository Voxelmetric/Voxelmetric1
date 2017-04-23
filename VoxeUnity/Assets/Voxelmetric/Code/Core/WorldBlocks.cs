using System;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

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
        public BlockData Get(ref Vector3Int pos)
        {
            // Return air for chunk that do not exist
            Chunk chunk = world.chunks.Get(ref pos);
            if (chunk==null)
                return BlockProvider.AirBlock;

            int xx = Helpers.Mod(pos.x,Env.ChunkSize);
            int yy = Helpers.Mod(pos.y,Env.ChunkSize);
            int zz = Helpers.Mod(pos.z,Env.ChunkSize);

            return chunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz));
        }

        public BlockData Get(Vector3Int pos)
        {
            // Return air for chunk that do not exist
            Chunk chunk = world.chunks.Get(ref pos);
            if (chunk == null)
                return BlockProvider.AirBlock;

            int xx = Helpers.Mod(pos.x, Env.ChunkSize);
            int yy = Helpers.Mod(pos.y, Env.ChunkSize);
            int zz = Helpers.Mod(pos.z, Env.ChunkSize);

            return chunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz));
        }

        /// <summary>
        /// Retrives the block at given world coordinates
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        public Block GetBlock(ref Vector3Int pos)
        {
            // Return air for chunk that do not exist
            Chunk chunk = world.chunks.Get(ref pos);
            if (chunk==null)
                return world.blockProvider.BlockTypes[BlockProvider.AirType];

            int xx = Helpers.Mod(pos.x, Env.ChunkSize);
            int yy = Helpers.Mod(pos.y, Env.ChunkSize);
            int zz = Helpers.Mod(pos.z, Env.ChunkSize);

            BlockData blockData = chunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz));
            return world.blockProvider.BlockTypes[blockData.Type];
        }

        /// <summary>
        /// Sets the block data at given world coordinates
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void Set(ref Vector3Int pos, BlockData blockData)
        {
            Chunk chunk = world.chunks.Get(ref pos);
            if (chunk==null)
                return;

            int xx = Helpers.Mod(pos.x, Env.ChunkSize);
            int yy = Helpers.Mod(pos.y, Env.ChunkSize);
            int zz = Helpers.Mod(pos.z, Env.ChunkSize);

            chunk.blocks.SetInner(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz), blockData);
        }

        /// <summary>
        /// Sets the block data at given world coordinates. It does not perform any logic. It simply sets to block
        /// Use this function only for generating your terrain and structures
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRaw(ref Vector3Int pos, BlockData blockData)
        {
            Chunk chunk = world.chunks.Get(ref pos);
            if (chunk==null)
                return;

            int xx = Helpers.Mod(pos.x, Env.ChunkSize);
            int yy = Helpers.Mod(pos.y, Env.ChunkSize);
            int zz = Helpers.Mod(pos.z, Env.ChunkSize);

            chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(xx, yy, zz), blockData);
        }

        /// <summary>
        /// Sets blocks to a given value in a given range
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRange(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            // Let's make sure that ranges are okay
            int x1, x2, y1, y2, z1, z2;
            if (posFrom.x > posTo.x)
            {
                x1 = posTo.x;
                x2 = posFrom.x;
            }
            else
            {
                x1 = posFrom.x;
                x2 = posTo.x;
            }
            if (posFrom.y > posTo.y)
            {
                y1 = posTo.y;
                y2 = posFrom.y;
            }
            else
            {
                y1 = posFrom.y;
                y2 = posTo.y;
            }
            if (posFrom.z > posTo.z)
            {
                z1 = posTo.z;
                z2 = posFrom.z;
            }
            else
            {
                z1 = posFrom.z;
                z2 = posTo.z;
            }
            Vector3Int pFrom = new Vector3Int(x1, y1, z1);
            Vector3Int pTo = new Vector3Int(x2, y2, z2);

            Vector3Int chunkPosFrom = Chunk.ContainingChunkPos(ref pFrom);
            Vector3Int chunkPosTo = Chunk.ContainingChunkPos(ref pTo);

            // Update all chunks in range
            int minY = Helpers.Mod(pFrom.y+chunkPosFrom.y,Env.ChunkSize);
            for (int cy = chunkPosFrom.y; cy<=chunkPosTo.y; cy += Env.ChunkSize, minY = 0)
            {
                int maxY = Helpers.MakeChunkCoordinate(minY);
                maxY = Helpers.Mod(Math.Min(maxY+cy-1, pTo.y),Env.ChunkSize);
                int minZ = Helpers.Mod(pFrom.z+chunkPosFrom.z,Env.ChunkSize);

                for (int cz = chunkPosFrom.z; cz<=chunkPosTo.z; cz += Env.ChunkSize, minZ = 0)
                {
                    int maxZ = Helpers.MakeChunkCoordinate(minZ);
                    maxZ = Helpers.Mod(Math.Min(maxZ+cz-1, pTo.z),Env.ChunkSize);
                    int minX = Helpers.Mod(pFrom.x+chunkPosFrom.x,Env.ChunkSize);

                    for (int cx = chunkPosFrom.x; cx<=chunkPosTo.x; cx += Env.ChunkSize, minX = 0)
                    {
                        Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                        Chunk chunk = world.chunks.Get(ref chunkPos);
                        if (chunk==null)
                            continue;

                        int maxX = Helpers.MakeChunkCoordinate(minX);
                        maxX = Helpers.Mod(Math.Min(maxX+cx-1, pTo.x),Env.ChunkSize);

                        Vector3Int from = new Vector3Int(minX, minY, minZ);
                        Vector3Int to = new Vector3Int(maxX, maxY, maxZ);
                        chunk.blocks.SetRange(ref from, ref to, blockData);
                    }
                }
            }
        }

        /// <summary>
        /// Sets the block data at given world coordinates, updates the chunk and its
        /// neighbors if the Update chunk flag is true or not set.
        /// </summary>
        /// <param name="pos">Global position of the block</param>
        /// <param name="blockData">The block be placed</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        public void Modify(ref Vector3Int pos, BlockData blockData, bool setBlockModified)
        {
            Chunk chunk = world.chunks.Get(ref pos);
            if (chunk==null)
                return;

            Vector3Int vector3Int = new Vector3Int(
                Helpers.Mod(pos.x, Env.ChunkSize),
                Helpers.Mod(pos.y, Env.ChunkSize),
                Helpers.Mod(pos.z, Env.ChunkSize)
                );

            chunk.blocks.Modify(ref vector3Int, blockData, setBlockModified);
        }

        /// <summary>
        /// Queues a modification of blocks in a given range
        /// </summary>
        /// <param name="posFrom">Starting positon in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        public void ModifyRange(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData, bool setBlockModified)
        {
            // Let's make sure that ranges are okay
            int x1, x2, y1, y2, z1, z2;
            if (posFrom.x>posTo.x)
            {
                x1 = posTo.x;
                x2 = posFrom.x;
            }
            else
            {
                x1 = posFrom.x;
                x2 = posTo.x;
            }
            if (posFrom.y > posTo.y)
            {
                y1 = posTo.y;
                y2 = posFrom.y;
            }
            else
            {
                y1 = posFrom.y;
                y2 = posTo.y;
            }
            if (posFrom.z > posTo.z)
            {
                z1 = posTo.z;
                z2 = posFrom.z;
            }
            else
            {
                z1 = posFrom.z;
                z2 = posTo.z;
            }
            Vector3Int pFrom = new Vector3Int(x1, y1, z1);
            Vector3Int pTo = new Vector3Int(x2, y2, z2);

            Vector3Int chunkPosFrom = Chunk.ContainingChunkPos(ref pFrom);
            Vector3Int chunkPosTo = Chunk.ContainingChunkPos(ref pTo);

            // Update all chunks in range
            int minY = Helpers.Mod(pFrom.y+chunkPosFrom.y,Env.ChunkSize);
            for (int cy = chunkPosFrom.y; cy<=chunkPosTo.y; cy += Env.ChunkSize, minY = 0)
            {
                int maxY = Helpers.MakeChunkCoordinate(minY);
                maxY = Helpers.Mod(Math.Min(maxY+cy-1, pTo.y),Env.ChunkSize);
                int minZ = Helpers.Mod(pFrom.z+chunkPosFrom.z,Env.ChunkSize);

                for (int cz = chunkPosFrom.z; cz<=chunkPosTo.z; cz += Env.ChunkSize, minZ = 0)
                {
                    int maxZ = Helpers.MakeChunkCoordinate(minZ);
                    maxZ = Helpers.Mod(Math.Min(maxZ+cz-1, pTo.z),Env.ChunkSize);
                    int minX = Helpers.Mod(pFrom.x+chunkPosFrom.x,Env.ChunkSize);

                    for (int cx = chunkPosFrom.x; cx<=chunkPosTo.x; cx += Env.ChunkSize, minX = 0)
                    {
                        Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                        Chunk chunk = world.chunks.Get(ref chunkPos);
                        if (chunk==null)
                            continue;

                        int maxX = Helpers.MakeChunkCoordinate(minX);
                        maxX = Helpers.Mod(Math.Min(maxX+cx-1, pTo.x),Env.ChunkSize);

                        Vector3Int from = new Vector3Int(minX, minY, minZ);
                        Vector3Int to = new Vector3Int(maxX, maxY, maxZ);
                        chunk.blocks.ModifyRange(ref from, ref to, blockData, setBlockModified);
                    }
                }
            }
        }
    }
}