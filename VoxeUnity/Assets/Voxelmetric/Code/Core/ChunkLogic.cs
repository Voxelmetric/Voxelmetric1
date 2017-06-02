using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public sealed class ChunkLogic
    {
        private readonly Chunk chunk;
        private float randomUpdateTime;
        private readonly List<BlockAndTimer> scheduledUpdates = new List<BlockAndTimer>();

        public ChunkLogic(Chunk chunk)
        {
            this.chunk = chunk;
            Reset();
        }

        public void Reset()
        {
            randomUpdateTime = 0;
            scheduledUpdates.Clear();
        }

        public void Update()
        {
            randomUpdateTime += Time.deltaTime;
            if (randomUpdateTime>=chunk.world.config.randomUpdateFrequency)
            {
                randomUpdateTime = 0;

                Vector3Int randomVector3Int = new Vector3Int(
                    Voxelmetric.resources.random.Next(0, Env.ChunkSize),
                    Voxelmetric.resources.random.Next(0, Env.ChunkSize),
                    Voxelmetric.resources.random.Next(0, Env.ChunkSize)
                    );

                chunk.blocks.GetBlock(ref randomVector3Int).RandomUpdate(chunk, ref randomVector3Int);

                // Process Scheduled Updates
                for (int i = 0; i<scheduledUpdates.Count; i++)
                {
                    scheduledUpdates[i] = new BlockAndTimer(
                        scheduledUpdates[i].pos,
                        scheduledUpdates[i].time-chunk.world.config.randomUpdateFrequency
                        );

                    if (scheduledUpdates[i].time<=0)
                    {
                        Vector3Int pos = scheduledUpdates[i].pos;
                        Block block = chunk.blocks.GetBlock(ref pos);
                        block.ScheduledUpdate(chunk, ref pos);
                        scheduledUpdates.RemoveAt(i);
                        i--;
                    }
                }
            }

        }

        public void AddScheduledUpdate(Vector3Int vector3Int, float time)
        {
            scheduledUpdates.Add(new BlockAndTimer(vector3Int, time));
        }
    }
}