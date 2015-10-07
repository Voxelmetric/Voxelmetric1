using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour
{

    public World world;

    [Range(1, 32)]
    public int chunkLoadRadius = 8;
    BlockPos[] chunkPositions;

    //The distance is measured in blocks
    [Range(1, 64)]
    public int DistanceToDeleteChunks = (int)(8 * 1.25f);

    [Range(10, 100)]
    public int WaitBetweenDeletes = 10;
    //Recommend setting this to at least 2 when LightSceneOnStart is enabled 
    [Range(0, 100)]
    public int WaitBetweenChunkGen = 1;

    int deleteTimer = 0;
    int chunkGenTimer = 0;

    void Start() {
        chunkPositions = ChunkLoadOrder.ChunkPositions(chunkLoadRadius);
    }

    // Update is called once per frame
    void Update()
    {
        if (deleteTimer == WaitBetweenDeletes)
        {
            DeleteChunks();
            deleteTimer = 0;
            return;
        }
        else
        {
            deleteTimer++;
        }

        if (chunkGenTimer == WaitBetweenChunkGen)
        {
            FindChunksAndLoad();
            chunkGenTimer = 0;
            return;
        }
        else
        {
            chunkGenTimer++;
        }
    }

    void DeleteChunks()
    {
        var chunksToDelete = new List<BlockPos>();
        foreach (var chunk in world.chunks)
        {
            Vector3 chunkPos = chunk.Key; 
            float distance = Vector3.Distance(
                new Vector3(chunkPos.x, 0, chunkPos.z),
                new Vector3(transform.position.x, 0, transform.position.z));

            if (distance > DistanceToDeleteChunks * Config.Env.ChunkSize * Config.Env.BlockSize)
                chunksToDelete.Add(chunk.Key);
        }

        foreach (var chunk in chunksToDelete)
            world.DestroyChunk(chunk);

    }

    bool FindChunksAndLoad()
    {
        //Cycle through the array of positions
        for (int i = 0; i < chunkPositions.Length; i++)
        {
            //Get the position of this gameobject to generate around
            BlockPos playerPos = ((BlockPos)transform.position).ContainingChunkCoordinates();

            //translate the player position and array position into chunk position
            BlockPos newChunkPos = new BlockPos(
                chunkPositions[i].x * Config.Env.ChunkSize + playerPos.x,
                0,
                chunkPositions[i].z * Config.Env.ChunkSize + playerPos.z
                );

            //Get the chunk in the defined position
            Chunk newChunk = world.GetChunk(newChunkPos);

            //If the chunk already exists and it's already
            //rendered or in queue to be rendered continue
            if (newChunk != null && newChunk.GetFlag(Chunk.Flag.loaded))
                continue;

            LoadChunkColumn(newChunkPos);
            return true;
        }

        return false;
    }

    public void LoadChunkColumn(BlockPos columnPosition)
    {
        //First create the chunk game objects in the world class
        //The world class wont do any generation when threaded chunk creation is enabled
        for (int y = world.config.minY; y <= world.config.maxY; y += Config.Env.ChunkSize)
        {
            for (int x = columnPosition.x - Config.Env.ChunkSize; x <= columnPosition.x + Config.Env.ChunkSize; x += Config.Env.ChunkSize)
            {
                for (int z = columnPosition.z - Config.Env.ChunkSize; z <= columnPosition.z + Config.Env.ChunkSize; z += Config.Env.ChunkSize)
                {
                    BlockPos pos = new BlockPos(x, y, z);
                    Chunk chunk = world.GetChunk(pos);
                    if (chunk == null)
                    {
                        world.CreateChunk(pos);
                    }
                }
            }
        }

        for (int y = world.config.maxY; y >= world.config.minY; y -= Config.Env.ChunkSize)
        {
            BlockPos pos = new BlockPos(columnPosition.x, y, columnPosition.z);
            Chunk chunk = world.GetChunk(pos);
            if (chunk != null)
            {
                chunk.SetFlag(Chunk.Flag.loaded, true);
            }

        }

        //Start the threaded chunk generation
        if (Config.Toggle.UseMultiThreading) {
            Thread thread = new Thread(() => { LoadChunkColumnInner(columnPosition); } );
            thread.Start();
        }
        else
        {
            LoadChunkColumnInner(columnPosition);
        }

    }

    void LoadChunkColumnInner(BlockPos columnPosition)
    {
        Chunk chunk;
        
        // Terrain generation can happen in another thread meaning that we will reach this point before the
        //thread completes, we need to wait for all the chunks we depend on to finish generating before we
        //can calculate any light spread or render the chunk
        if (Config.Toggle.UseMultiThreading)
        {
            for (int y = world.config.maxY; y >= world.config.minY; y -= Config.Env.ChunkSize)
            {
                for (int x = -Config.Env.ChunkSize; x <= Config.Env.ChunkSize; x += Config.Env.ChunkSize)
                {
                    for (int z = -Config.Env.ChunkSize; z <= Config.Env.ChunkSize; z += Config.Env.ChunkSize)
                    {
                        chunk = world.GetChunk(columnPosition.Add(x, y, z));
                        while (!chunk.GetFlag(Chunk.Flag.terrainGenerated))
                        {
                            Thread.Sleep(0);
                        }
                    }
                }
            }
        }

        //Render chunk
        for (int y = world.config.maxY; y >= world.config.minY; y -= Config.Env.ChunkSize)
        {
            chunk = world.GetChunk(columnPosition.Add(0, y, 0));

            //Disabled untill lighting is revisited
            //if(Config.Toggle.LightSceneOnStart){
            //    BlockLight.FloodLightChunkColumn(world, chunk);
            //}

            chunk.UpdateNow();
        }
    }

}
