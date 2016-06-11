using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public class LoadChunks : MonoBehaviour
    {
        public World world;

        [Range(4, 64)]
        public int chunkLoadRadius = 8;
        BlockPos[] chunkPositions;

        //The distance is measured in chunks
        [Range(4, 64)]
        public int DistanceToDeleteChunks = (int)(8 * 1.25f);
        private int distanceToDeleteInUnitsSquared;

        void Start()
        {
            chunkPositions = ChunkLoadOrder.ChunkPositions(chunkLoadRadius);
            distanceToDeleteInUnitsSquared = (int)(DistanceToDeleteChunks * Env.ChunkSize * Env.BlockSize);
            distanceToDeleteInUnitsSquared *= distanceToDeleteInUnitsSquared;
        }

        void Update()
        {
            DeleteChunks();
            CreateChunks();
        }

        private void DeleteChunks()
        {
            int posX = Mathf.FloorToInt(transform.position.x);
            int posZ = Mathf.FloorToInt(transform.position.z);

            foreach (var pos in world.chunks.posCollection)
            {
                int xd = posX - pos.x;
                int yd = posZ - pos.z;

                if ((xd * xd + yd * yd) > distanceToDeleteInUnitsSquared)
                {
                    Chunk chunk = world.chunks.Get(pos);
                    chunk.RequestRemoval();
                }
            }
        }

        private void CreateChunks()
        {
            // Cycle through the array of positions
            for (int i = 0; i < chunkPositions.Length; i++)
            {
                // Get the position of this gameobject to generate around
                BlockPos playerPos = ((BlockPos)transform.position).ContainingChunkCoordinates();

                // Translate the player position and array position into chunk position
                BlockPos newChunkPos = new BlockPos(
                    chunkPositions[i].x*Env.ChunkSize+playerPos.x,
                    0,
                    chunkPositions[i].z*Env.ChunkSize+playerPos.z
                    );

                for (int y = world.config.minY; y<=world.config.maxY; y += Env.ChunkSize)
                    world.chunks.CreateChunk(new BlockPos(newChunkPos.x, newChunkPos.y + y, newChunkPos.z));
            }
        }
    }
}
