using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour
{

    public World world;

    int timer = 0;

    // Update is called once per frame
    void Update()
    {
        if (DeleteChunks())
            return;

        FindChunksToLoad();
    }

    bool DeleteChunks()
    {

        if (timer == Config.Env.WaitBetweenDeletes)
        {
            var chunksToDelete = new List<BlockPos>();
            foreach (var chunk in world.chunks)
            {
                float distance = Vector3.Distance(
                    new Vector3(chunk.Value.pos.x, 0, chunk.Value.pos.z),
                    new Vector3(transform.position.x, 0, transform.position.z));

                if (distance > Config.Env.DistanceToDeleteChunks)
                    chunksToDelete.Add(chunk.Key);
            }

            foreach (var chunk in chunksToDelete)
                world.DestroyChunk(chunk);

            timer = 0;
            return true;
        }

        timer++;
        return false;
    }

    bool FindChunksToLoad()
    {
        //Cycle through the array of positions
        for (int i = 0; i < Config.Env.ChunksToLoad; i++)
        {
            //Get the position of this gameobject to generate around
            BlockPos playerPos = ((BlockPos)transform.position).ContainingChunkCoordinates();

            //translate the player position and array position into chunk position
            BlockPos newChunkPos = new BlockPos(
                Data.chunkLoadOrder[i].x * Config.Env.ChunkSize + playerPos.x,
                0,
                Data.chunkLoadOrder[i].z * Config.Env.ChunkSize + playerPos.z
                );

            //Get the chunk in the defined position
            Chunk newChunk = world.GetChunk(newChunkPos);

            //If the chunk already exists and it's already
            //rendered or in queue to be rendered continue
            if (newChunk != null && (newChunk.rendered || newChunk.busy))
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
        for (int y = Config.Env.WorldMaxY; y >= Config.Env.WorldMinY; y -= Config.Env.ChunkSize)
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


        //Now we can start the threaded chunk generation
        Thread thread = new Thread(() =>
        {
            Chunk chunk;

            for (int y = Config.Env.WorldMaxY; y >= Config.Env.WorldMinY; y -= Config.Env.ChunkSize)
            {
                for (int x = - Config.Env.ChunkSize; x <= Config.Env.ChunkSize; x += Config.Env.ChunkSize)
                {
                    for (int z = - Config.Env.ChunkSize; z <= Config.Env.ChunkSize; z += Config.Env.ChunkSize)
                    {
                        chunk = world.GetChunk(columnPosition.Add(x, y, z));
                        while (!chunk.terrainGenerated)
                        {
                            Thread.Sleep(0);
                        }
                    }
                }
            }

            if (Config.Toggle.LightSceneOnStart)
            {
                //reset light
                chunk = world.GetChunk(columnPosition);
                BlockLight.ResetLightChunkColumn(world, chunk);

                //Flood light
                for (int y = Config.Env.WorldMaxY; y >= Config.Env.WorldMinY; y -= Config.Env.ChunkSize)
                {
                    //Threading issues with the flood lighting will crash unity and possibly your sound card :S
                    //chunk = world.GetChunk(columnPosition.Add(0, y, 0));
                    //BlockLight.FloodLightChunkColumn(world, chunk);
                }
            }
            
            //Render chunk
            for (int y = Config.Env.WorldMaxY; y >= Config.Env.WorldMinY; y -= Config.Env.ChunkSize)
            {
                chunk = world.GetChunk(columnPosition.Add(0, y, 0));
                chunk.UpdateChunk();
            }
        });

        thread.Start();

    }

}
