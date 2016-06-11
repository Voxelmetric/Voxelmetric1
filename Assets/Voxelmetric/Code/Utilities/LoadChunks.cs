using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public class LoadChunks : MonoBehaviour
    {
        public World world;
        BlockPos[] m_chunkPositions;

        // Distance in chunks for loading chunks
        [Range(4, 62)]
        public int ChunkLoadRadius = 6;
        // Distance in chunks for unloading chunks
        [Range(5, 64)]
        public int DistanceToDeleteChunks = 5;

        void Awake()
        {
            m_chunkPositions = ChunkLoadOrder.ChunkPositions(ChunkLoadRadius);
        }

        void Update()
        {
            UpdateDistanceToDelete();

            DeleteChunks();
            CreateChunks();
        }

        private void UpdateDistanceToDelete()
        {
            // Make sure the value is always correct
            if (DistanceToDeleteChunks<=ChunkLoadRadius)
                DistanceToDeleteChunks = ChunkLoadRadius+1;
        }

        private void DeleteChunks()
        {
            int posX = Mathf.FloorToInt(transform.position.x);
            int posZ = Mathf.FloorToInt(transform.position.z);

            foreach (var pos in world.chunks.posCollection)
            {
                int xd = Mathf.Abs((posX - pos.x)>>Env.ChunkPower);
                int zd = Mathf.Abs((posZ - pos.z)>>Env.ChunkPower);
                if (xd*xd+zd*zd>=DistanceToDeleteChunks*DistanceToDeleteChunks)
                {
                    Chunk chunk = world.chunks.Get(pos);
                    chunk.RequestRemoval();
                }
            }
        }

        private void CreateChunks()
        {
            // Cycle through the array of positions
            for (int i = 0; i < m_chunkPositions.Length; i++)
            {
                // Get the position of this gameobject to generate around
                BlockPos playerPos = ((BlockPos)transform.position).ContainingChunkCoordinates();

                // Translate the player position and array position into chunk position
                BlockPos newChunkPos = new BlockPos(
                    m_chunkPositions[i].x*Env.ChunkSize+playerPos.x,
                    0,
                    m_chunkPositions[i].z*Env.ChunkSize+playerPos.z
                    );

                for (int y = world.config.minY; y<=world.config.maxY; y += Env.ChunkSize)
                    world.chunks.CreateChunk(new BlockPos(newChunkPos.x, newChunkPos.y + y, newChunkPos.z));
            }
        }
    }
}
