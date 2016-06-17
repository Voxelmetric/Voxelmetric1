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

        public bool Diag_DrawWorldBounds = false;
        public bool Diag_DrawLoadRange = false;

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
            // Get the position of this gameobject to generate around
            BlockPos playerPos = ((BlockPos)transform.position).ContainingChunkCoordinates();

            // Cycle through the array of positions
            for (int i = 0; i < m_chunkPositions.Length; i++)
            {
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

        private void OnDrawGizmosSelected()
        {
            int size = Mathf.FloorToInt(Env.ChunkSize*Env.BlockSize);
            int halfSize = size>>1;
            int quaterSize = size>>2;

            int posX = Mathf.FloorToInt(transform.position.x);
            int posZ = Mathf.FloorToInt(transform.position.z);

            if (world!=null && world.chunks!=null && (Diag_DrawWorldBounds || Diag_DrawLoadRange))
            {
                foreach (Chunk chunk in world.chunks.chunkCollection)
                {
                    if (Diag_DrawWorldBounds)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(chunk.WorldBounds.center, chunk.WorldBounds.size);
                    }

                    if (Diag_DrawLoadRange)
                    {
                        BlockPos pos = chunk.pos;
                        int xd = Mathf.Abs((posX-pos.x)>>Env.ChunkPower);
                        int zd = Mathf.Abs((posZ-pos.z)>>Env.ChunkPower);
                        if (xd*xd+zd*zd>=DistanceToDeleteChunks*DistanceToDeleteChunks)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.pos.x+halfSize,
                                    0,
                                    chunk.pos.z+halfSize),
                                new Vector3(size-0.05f, 0, size-0.05f)
                                );
                        }
                        else
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.pos.x+halfSize,
                                    0,
                                    chunk.pos.z+halfSize),
                                new Vector3(size-0.05f, 0, size-0.05f)
                                );
                        }

                        // Show generated chunks
                        if(chunk.IsGenerated)
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireCube(
                                new Vector3(
                                    chunk.pos.x+halfSize,
                                    0,
                                    chunk.pos.z+halfSize),
                                new Vector3(quaterSize-0.05f, 0, quaterSize-0.05f)
                                );
                        }
                    }
                }
            }
        }
    }
}
