using System.Collections.Generic;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public class WorldChunks
    {
        World world;
        Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();

        public ICollection<Chunk> chunkCollection
        {
            get { return chunks.Values; }
        }

        public ICollection<BlockPos> posCollection
        {
            get { return chunks.Keys; }
        }

        public WorldChunks(World world)
        {
            this.world = world;
        }

        public Chunk this[int x, int y, int z]
        {
            get { return this[new BlockPos(x, y, z)]; }
        }

        public Chunk this[BlockPos pos]
        {
            get { return Get(pos); }
        }

        /// <summary> Returns the chunk at the given position </summary>
        /// <param name="pos">Position of the chunk or of a block within the chunk</param>
        /// <returns>The chunk that contains the given block position or null if there is none</returns>
        public Chunk Get(BlockPos pos)
        {
            pos = pos.ContainingChunkCoordinates();

            Chunk containerChunk;
            chunks.TryGetValue(pos, out containerChunk);

            return containerChunk;
        }

        public void Set(BlockPos pos, Chunk chunk)
        {
            pos = pos.ContainingChunkCoordinates();
            chunks[pos] = chunk;
        }

        public void Remove(BlockPos pos)
        {
            chunks.Remove(pos);
        }

        /// <summary>Instantiates a chunk at a given possiion</summary>
        /// <param name="pos">The world position to create this chunk.</param>
        /// <returns>A new chunk<returns>
        public Chunk CreateChunk(BlockPos pos)
        {
            // Let's keep it withing allowed world bounds
            BlockPos chunkPos = pos.ContainingChunkCoordinates();
            if (chunkPos.y>world.config.maxY || chunkPos.y<world.config.minY)
                return null;

            // Don't recreate the chunk if it already exists
            Chunk chunk = Get(chunkPos);
            if(chunk!=null)
                return chunk;
            
            // Create a new chunk
            chunk = Chunk.CreateChunk(world, chunkPos);
            chunks.Add(chunkPos, chunk);
            chunk.RequestGenerate();
            return chunk;
        }
        
    }
}
