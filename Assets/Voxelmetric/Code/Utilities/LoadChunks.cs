using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour
{

    public World world;
    public bool generateTerrain = true;
    private BlockPos objectPos;

    [Range(1, 64)]
    public int chunkLoadRadius = 8;
    BlockPos[] chunkPositions;

    //The distance is measured in chunks
    [Range(1, 64)]
    public int DistanceToDeleteChunks = (int)(8 * 1.25f);
    private int distanceToDeleteInUnitsSquared;

    void Start()
    {
        chunkPositions = ChunkLoadOrder.ChunkPositions(chunkLoadRadius);
        distanceToDeleteInUnitsSquared = (int)(DistanceToDeleteChunks * Config.Env.ChunkSize * Config.Env.BlockSize);
        distanceToDeleteInUnitsSquared *= distanceToDeleteInUnitsSquared;
    }

    void Update()
    {
        objectPos = transform.position;

        if (world.chunksLoop.ChunksInProgress > 0)
        {
            return;
        }

        Profiler.BeginSample("delete");
        DeleteChunks();
        Profiler.EndSample();
        Profiler.BeginSample("find and load");
        FindChunksAndLoad();
        Profiler.EndSample();
    }

    void DeleteChunks()
    {
        int posX = objectPos.x;
        int posZ = objectPos.z;

        foreach (var pos in world.chunks.posCollection)
        {
            int xd = posX - pos.x;
            int yd = posZ - pos.z;

            if ((xd * xd + yd * yd) > distanceToDeleteInUnitsSquared)
            {
                Chunk chunk = world.chunks.Get(pos);
                if(!chunk.logic.GetFlag(Flag.markedForDeletion))
                    chunk.MarkForDeletion();
            }
        }
    }

    void FindChunksAndLoad()
    {
        //Cycle through the array of positions
        for (int i = 0; i < chunkPositions.Length; i++)
        {
            //Get the position of this gameobject to generate around
            BlockPos playerPos = objectPos.ContainingChunkCoordinates();

            //translate the player position and array position into chunk position
            BlockPos newChunkPos = new BlockPos(
                chunkPositions[i].x * Config.Env.ChunkSize + playerPos.x,
                0,
                chunkPositions[i].z * Config.Env.ChunkSize + playerPos.z
                );

            //Get the chunk in the defined position
            Chunk newChunk = world.chunks.Get(newChunkPos);

            //If the chunk already exists and it's already
            //rendered or in queue to be rendered continue
            if (newChunk != null && newChunk.stage != Stage.created)
                continue;

            for (int y = world.config.minY; y <= world.config.maxY; y += Config.Env.ChunkSize)
                world.chunks.CreateChunkAndNeighbors(new BlockPos(newChunkPos.x, y, newChunkPos.z));

            Debug.Log(i);
            return;
        }

        Debug.Log(chunkPositions.Length);

    }
}
