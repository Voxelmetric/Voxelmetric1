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

        if (timer == 10)
        {
            var chunksToDelete = new List<BlockPos>();
            foreach (var chunk in world.chunks)
            {
                float distance = Vector3.Distance(
                    new Vector3(chunk.Value.pos.x, 0, chunk.Value.pos.z),
                    new Vector3(transform.position.x, 0, transform.position.z));

                if (distance > 256)
                    chunksToDelete.Add(chunk.Key);
            }

            foreach (var chunk in chunksToDelete)
                world.DestroyChunk(chunk.x, chunk.y, chunk.z);

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
        for (int i = 0; i < Data.chunkLoadOrder.Length; i++)
        {
            //Get the position of this gameobject to generate around
            BlockPos playerPos = new BlockPos(
                Mathf.FloorToInt(transform.position.x / Config.ChunkSize) * Config.ChunkSize,
                Mathf.FloorToInt(transform.position.y / Config.ChunkSize) * Config.ChunkSize,
                Mathf.FloorToInt(transform.position.z / Config.ChunkSize) * Config.ChunkSize
                );

            //translate the player position and array position into chunk position
            BlockPos newChunkPos = new BlockPos(
                Data.chunkLoadOrder[i].x * Config.ChunkSize + playerPos.x,
                0,
                Data.chunkLoadOrder[i].z * Config.ChunkSize + playerPos.z
                );

            //Get the chunk in the defined position
            Chunk newChunk = world.GetChunk(
                newChunkPos.x, newChunkPos.y, newChunkPos.z);

            //If the chunk already exists and it's already
            //rendered or in queue to be rendered continue
            if (newChunk != null  && (newChunk.rendered || updateList.Contains(newChunkPos)))
                continue;

            //load a column of chunks in this position
            for (int y = 4; y >= -4; y--)
            {

                for (int x = newChunkPos.x - Config.ChunkSize; x <= newChunkPos.x + Config.ChunkSize; x += Config.ChunkSize)
                {
                    for (int z = newChunkPos.z - Config.ChunkSize; z <= newChunkPos.z + Config.ChunkSize; z += Config.ChunkSize)
                    {
                        buildList.Add(new BlockPos(x, y * Config.ChunkSize, z));
                    }
                }
                updateList.Add(new BlockPos( newChunkPos.x, y * Config.ChunkSize, newChunkPos.z));
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
                Chunk chunk = world.GetChunk(pos.x, pos.y, pos.z);
                if(chunk==null){
                    world.CreateChunk(pos.x, pos.y, pos.z);
                }
                buildList.RemoveAt(0);

                if (pos.y == -64 && chunk == null)
                {
                    BlockLight.ResetLightChunkColumn(world, world.GetChunk(pos.x, pos.y, pos.z));
                    return;
                }
            }
        }

        if ( updateList.Count!=0)
        {
            int count = updateList.Count;
            for (int i = 0; i < count && i<3; i++)
            {
                var pos = updateList[0];
                Chunk chunk = world.GetChunk(updateList[0].x, updateList[0].y, updateList[0].z);
                if (chunk != null){

                    //Removed from master until it's faster
                    //Profiler.BeginSample("flood light setup");
                    //if (pos.y == -64)
                    //    BlockLight.FloodLightChunkColumn(world, chunk);
                    //Profiler.EndSample();

                    chunk.update = true;
                }
                updateList.RemoveAt(0);
            }
        }
    }

}
