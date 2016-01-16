using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public enum Flag { loadStarted, busy, meshReady, contentsGenerated, loadComplete, chunkModified, updateSoon, updateNow }

public class ChunkLogic {

    protected Chunk chunk;
    protected float randomUpdateTime = 0;

    public Hashtable flags = new Hashtable();

    protected List<BlockAndTimer> scheduledUpdates = new List<BlockAndTimer>();

    public ChunkLogic(Chunk chunk)
    {
        this.chunk = chunk;
    }

    public bool GetFlag(object key)
    {
        if (!flags.ContainsKey(key))
        {
            return false;
        }
        return (bool)flags[key];
    }

    public T GetFlag<T>(object key) where T : new()
    {
        if (!flags.ContainsKey(key))
        {
            return new T();
        }
        return (T)flags[key];
    }

    public void SetFlag(object key, object value)
    {
        if (flags.ContainsKey(key))
        {
            flags.Remove(key);
        }

        flags.Add(key, value);
    }

    public virtual void TimedUpdated()
    {
        if (!GetFlag(Flag.loadComplete))
        {
            return;
        }

        randomUpdateTime += Time.fixedDeltaTime;
        if (randomUpdateTime >= chunk.world.config.randomUpdateFrequency)
        {
            randomUpdateTime = 0;

            BlockPos randomPos = chunk.pos;
            randomPos.x += Voxelmetric.resources.random.Next(0, 16);
            randomPos.y += Voxelmetric.resources.random.Next(0, 16);
            randomPos.z += Voxelmetric.resources.random.Next(0, 16);

            chunk.blocks.Get(randomPos).RandomUpdate(chunk, randomPos, randomPos + chunk.pos);

            //Process Scheduled Updates
            for (int i = 0; i < scheduledUpdates.Count; i++)
            {
                scheduledUpdates[i] = new BlockAndTimer(scheduledUpdates[i].pos, scheduledUpdates[i].time - chunk.world.config.randomUpdateFrequency);
                if (scheduledUpdates[i].time <= 0)
                {
                    Block block = chunk.blocks.Get(scheduledUpdates[i].pos);
                    block.ScheduledUpdate(chunk, scheduledUpdates[i].pos, scheduledUpdates[i].pos + chunk.pos);
                    scheduledUpdates.RemoveAt(i);
                    i--;
                }
            }

            if (GetFlag(Flag.updateSoon))
            {
                chunk.render.UpdateChunk();
                SetFlag(Flag.updateSoon, false);
                SetFlag(Flag.updateNow, false);
            }
        }

    }

    public void AddScheduledUpdate(BlockPos pos, float time)
    {
        scheduledUpdates.Add(new BlockAndTimer(pos, time));
    }

    public void ResetContent()
    {
        flags.Clear();
        scheduledUpdates.Clear();
    }

}
