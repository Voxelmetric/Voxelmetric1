using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public class WorldChunks
    {
        World world;
        Dictionary<Vector3Int, Chunk> chunks = new Dictionary<Vector3Int, Chunk>();

        /*[ThreadStatic]
        private static Vector3Int lastChunkPos;
        [ThreadStatic]
        private static Chunk lastChunk;*/

        public ICollection<Chunk> chunkCollection
        {
            get { return chunks.Values; }
        }

        public ICollection<Vector3Int> posCollection
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

        /// <summary> Returns the chunk at the given position </summary>
        /// <param name="pos">Position of the chunk in the world coordinates</param>
        /// <returns>The chunk that contains the given block position or null if there is none</returns>
        public Chunk Get(ref Vector3Int pos)
        {
            Vector3Int p = Chunk.ContainingChunkPos(ref pos);

            // If we previously searched for this chunk there is no need to look it up again
            /*if (p == lastChunkPos && lastChunk != null)
                return lastChunk;

            lastChunkPos = p;*/

            Chunk containerChunk;
            chunks.TryGetValue(p, out containerChunk);

            return containerChunk;
        }

        public bool Set(ref Vector3Int pos, Chunk chunk)
        {
            Assert.IsTrue(Helpers.IsMainThread);
            Vector3Int p = Chunk.ContainingChunkPos(ref pos);

            // Let's keep it within allowed world bounds
            if (!world.IsCoordInsideWorld(ref p))
                return false;

            chunks[p] = chunk;
            return true;
        }

        /// <summary>Removes a given chunk from the world</summary>
        /// <param name="chunk">Chunk to be removed from the world</param>
        public void RemoveChunk(Chunk chunk)
        {
            Assert.IsTrue(Helpers.IsMainThread);
            Assert.IsNotNull(chunk);
            /*if (chunk == lastChunk)
                lastChunk = null;*/

            Chunk.RemoveChunk(chunk);
            chunks.Remove(chunk.pos);
        }

        /// <summary>Instantiates a new chunk at a given position. If the chunk already exists, it returns it</summary>
        /// <param name="pos">Position to create this chunk on in the world coordinates.</param>
        /// <param name="chunk">Chunk at a given world position</param>
        /// <returns>True if a new chunk was created. False otherwise</returns>
        public bool CreateOrGetChunk(ref Vector3Int pos, out Chunk chunk)
        {
            Assert.IsTrue(Helpers.IsMainThread);
            Vector3Int p = Chunk.ContainingChunkPos(ref pos);

            // Let's keep it within allowed world bounds
            if (!world.IsCoordInsideWorld(ref p))
            {
                chunk = null;
                return false;
            }

            /*// If we previously searched for this chunk there is no need to look it up again
            if (pos==lastChunkPos && lastChunk!=null)
            {
                chunk = lastChunk;
                return false;
            }

            lastChunkPos = pos;*/

            // Don't recreate the chunk if it already exists
            chunk = Get(ref p);
            if (chunk!=null)
                return false;

            // Create a new chunk
            chunk = Chunk.CreateChunk(world, p);

            lock (chunks)
            {
                chunks.Add(p, chunk);
            }

            return true;
        }

    }
}
