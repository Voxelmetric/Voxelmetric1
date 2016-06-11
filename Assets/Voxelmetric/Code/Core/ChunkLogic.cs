using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public sealed class ChunkLogic
    {
        private Chunk chunk;
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

        public void TimedUpdated()
        {
            randomUpdateTime += Time.fixedDeltaTime;
            if (randomUpdateTime>=chunk.world.config.randomUpdateFrequency)
            {
                randomUpdateTime = 0;

                BlockPos randomPos = chunk.pos;
                randomPos.x += Voxelmetric.resources.random.Next(0, Env.ChunkMask);
                randomPos.y += Voxelmetric.resources.random.Next(0, Env.ChunkMask);
                randomPos.z += Voxelmetric.resources.random.Next(0, Env.ChunkMask);

                chunk.blocks.Get(randomPos).RandomUpdate(chunk, randomPos, randomPos+chunk.pos);

                //Process Scheduled Updates
                for (int i = 0; i<scheduledUpdates.Count; i++)
                {
                    scheduledUpdates[i] = new BlockAndTimer(scheduledUpdates[i].pos,
                                                            scheduledUpdates[i].time-
                                                            chunk.world.config.randomUpdateFrequency);
                    if (scheduledUpdates[i].time<=0)
                    {
                        Block block = chunk.blocks.Get(scheduledUpdates[i].pos);
                        block.ScheduledUpdate(chunk, scheduledUpdates[i].pos, scheduledUpdates[i].pos+chunk.pos);
                        scheduledUpdates.RemoveAt(i);
                        i--;
                    }
                }
            }

        }

        public void AddScheduledUpdate(BlockPos pos, float time)
        {
            scheduledUpdates.Add(new BlockAndTimer(pos, time));
        }
    }
}