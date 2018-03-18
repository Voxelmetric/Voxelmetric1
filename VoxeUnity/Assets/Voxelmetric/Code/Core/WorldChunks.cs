using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    /// <summary>
    /// Chunk storage. All chunks in the world are stored here
    /// </summary>
    public class WorldChunks
    {
        private World world;
        private Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();
        
        public ICollection<Chunk> Chunks
        {
            get { return chunks.Values; }
        }

        public ICollection<Vector3Int> Positions
        {
            get { return chunks.Keys; }
        }

        public int Count
        {
            get { return chunks.Count; }
        }

        public WorldChunks(World world)
        {
            this.world = world;
        }

        /// <summary>Instantiates a new chunk at a given position</summary>
        /// <param name="pos">Position to create this chunk on in the world coordinates</param>
        /// <param name="chunk">Chunk at a given positon</param>
        /// <returns>Trus if a new chunk was created. False otherwise</returns>
        public bool Create(ref Vector3Int pos, out Chunk chunk)
        {
            Assert.IsTrue(Helpers.IsMainThread);

            chunk = null;

            // Transform the position into chunk coordinates
            Vector3Int chunkPos = Helpers.ContainingChunkPos(ref pos);

            // Let's keep it within allowed world bounds
            if (!world.IsCoordInsideWorld(ref chunkPos))
                return false;
            
            chunk = Get(ref chunkPos);
            if (chunk == null)
            {
                // Create a new chunk if it does not exist yet
                chunk = Chunk.Create(world, chunkPos);
                chunks.Add(chunkPos, chunk);
                return true;
            }

            return false;
        }

        /// <summary>Removes a chunk from the world</summary>
        /// <param name="chunk">Chunk to be removed from the world</param>
        public void Remove(Chunk chunk)
        {
            Assert.IsTrue(Helpers.IsMainThread);
            Assert.IsNotNull(chunk);

            Chunk.Remove(chunk);
            chunks.Remove(chunk.Pos);
        }

        /// <summary>Returns a chunk at a given position</summary>
        /// <param name="pos">Position of the chunk in the world coordinates</param>
        /// <returns>A chunk at a given position</returns>
        public Chunk Get(ref Vector3Int pos)
        {
            Assert.IsTrue(Helpers.ContainingChunkPos(ref pos) == pos);

            Chunk containerChunk;
            chunks.TryGetValue(pos, out containerChunk);
            return containerChunk;
        }
    }
}
