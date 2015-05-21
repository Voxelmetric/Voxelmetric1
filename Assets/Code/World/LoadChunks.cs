using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour {

    public World world;

    int timer = 0;

    List<BlockPos> updateList = new List<BlockPos>();
    List<BlockPos> buildList = new List<BlockPos>();

	// Update is called once per frame
	void Update () {
        if (DeleteChunks())
            return;

        FindChunksToLoad();

        LoadAndRenderChunks();
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
        if ( updateList.Count != 0 || buildList.Count!=0)
            return false;

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
            if (newChunk != null  && (newChunk.rendered || updateList.Contains(newChunkPos)))
                continue;

            //load a column of chunks in this position
            for (int y = Config.Env.WorldMaxY / Config.Env.ChunkSize; y >= Config.Env.WorldMinY / Config.Env.ChunkSize; y--)
            {

                for (int x = newChunkPos.x - Config.Env.ChunkSize; x <= newChunkPos.x + Config.Env.ChunkSize; x += Config.Env.ChunkSize)
                {
                    for (int z = newChunkPos.z - Config.Env.ChunkSize; z <= newChunkPos.z + Config.Env.ChunkSize; z += Config.Env.ChunkSize)
                    {
                        buildList.Add(new BlockPos(x, y * Config.Env.ChunkSize, z));
                    }
                }
                updateList.Add(new BlockPos( newChunkPos.x, y * Config.Env.ChunkSize, newChunkPos.z));
            }
            return true;
        }

        return true;
    }

    void LoadAndRenderChunks()
    {
        if (buildList.Count != 0)
        {
            int count = buildList.Count;
            for (int i = 0; i < count; i++)
            {
                var pos = buildList[0];
                Chunk chunk = world.GetChunk(pos);
                if(chunk==null){
                    world.CreateChunk(pos);
                }
                buildList.RemoveAt(0);

                if (Config.Toggle.BlockLighting)
                {
                    if (pos.y == Config.Env.WorldMinY && chunk == null)
                    {
                        BlockLight.ResetLightChunkColumn(world, world.GetChunk(pos));
                        return;
                    }
                }
            }
        }

        if ( updateList.Count!=0)
        {
            int count = updateList.Count;

            //This three would be a config entry but this will all change once we add threading 
            for (int i = 0; i < count && i<3; i++)
            {
                Chunk chunk = world.GetChunk(updateList[0]);
                if (chunk != null){

                    if (Config.Toggle.BlockLighting)
                    {
                        var pos = updateList[0];
                        if (pos.y == Config.Env.WorldMinY / Config.Env.ChunkSize)
                            BlockLight.FloodLightChunkColumn(world, chunk);
                    }

                    chunk.QueueUpdate();
                }
                updateList.RemoveAt(0);
            }
        }
    }

}
