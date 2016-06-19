using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public class WorldChunks
    {
        World world;
        Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        public ICollection<Chunk> chunkCollection
        {
            get { return chunks.Values; }
        }

        public ICollection<Vector3Int> posCollection
        {
            get { return chunks.Keys; }
        }

        public WorldChunks(World world)
        {
            this.world = world;
        }
        
        /// <summary> Returns the chunk at the given position </summary>
        /// <param name="pos">Position of the chunk in the world coordinates</param>
        /// <returns>The chunk that contains the given block position or null if there is none</returns>
        public Chunk Get(Vector3Int pos)
        {
            pos = pos.ContainingChunkCoordinates();

            Chunk containerChunk;
            chunks.TryGetValue(pos, out containerChunk);

            return containerChunk;
        }

        public void Set(Vector3Int pos, Chunk chunk)
        {
            pos = pos.ContainingChunkCoordinates();
            chunks[pos] = chunk;
        }

        /// <summary>Removes a given chunk from the world</summary>
        /// <param name="chunk">Chunk to be removed from the world
        public void RemoveChunk(Chunk chunk)
        {
            Assert.IsNotNull(chunk);

            Chunk.RemoveChunk(chunk);
            chunks.Remove(chunk.pos);
        }

        /// <summary>Instantiates a new chunk at a given position. If the chunk already exists, it returns it</summary>
        /// <param name="pos">Position to create this chunk on in the world coordinates.</param>
        /// <param name="chunk">Chunk at a given world position</param>
        /// <returns>True if a new chunk was created. False otherwise</returns>
        public bool CreateOrGetChunk(Vector3Int pos, out Chunk chunk, bool isDedicated)
        {
            // Let's keep it withing allowed world bounds
            Vector3Int chunkPos = pos.ContainingChunkCoordinates();
            if (chunkPos.y>world.config.maxY || chunkPos.y<world.config.minY)
            {
                chunk = null;
                return false;
            }

            // Don't recreate the chunk if it already exists
            chunk = Get(chunkPos);
            if (chunk!=null)
                return false;

            // Create a new chunk
            chunk = Chunk.CreateChunk(world, chunkPos, isDedicated);
            chunks.Add(chunkPos, chunk);
            return true;
        }
        
    }
}
