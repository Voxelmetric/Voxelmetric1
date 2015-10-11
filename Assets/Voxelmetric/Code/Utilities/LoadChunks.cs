using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour
{

    public World world;

    [Range(1, 64)]
    public int chunkLoadRadius = 8;
    BlockPos[] chunkPositions;

    //The distance is measured in chunks
    [Range(1, 64)]
    public int DistanceToDeleteChunks = (int)(8 * 1.25f);

    //Every WaitBetweenDeletes frames load chunks will stop to remove chunks beyond DistanceToDeleteChunks
    [Range(1, 100)]
    public int WaitBetweenDeletes = 10;

    //Every frame LoadChunks is not deleting chunks or finding chunks it will 
    [Range(1, 16)]
    public int ChunksToLoadPerFrame = 4;

    // Loads the top chunk of the column on its own rather than along with the ChunksToLoadPerFrame other chunks
    // This is useful because world generates the terrain for the column when the top chunk is loaded so this gives
    // the terrain generation a frame on its own
    public bool RenderChunksInSeparateFrame = true;

    int deleteTimer = 0;

    List<BlockPos> chunksToGenerate = new List<BlockPos>();

    void Start()
    {
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

        if (world.ChunksToRender.Count != 0)
        {
            for (int i = 0; i < ChunksToLoadPerFrame; i++)
            {
                if (world.ChunksToRender.Count == 0)
                {
                    break;
                }

                BlockPos pos = world.ChunksToRender[0];
                world.GetChunk(pos).UpdateChunk();
                world.ChunksToRender.RemoveAt(0);
            }

            if (RenderChunksInSeparateFrame)
            {
                return;
            }
        }

        if (chunksToGenerate.Count == 0)
        {
            FindChunksAndLoad();
        }

        for (int i = 0; i < ChunksToLoadPerFrame; i++)
        {
            if (chunksToGenerate.Count == 0)
            {
                break;
            }

            BlockPos pos = chunksToGenerate[0];
            world.CreateAndLoadChunk(pos);
            chunksToGenerate.RemoveAt(0);
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

            if (chunksToGenerate.Contains(newChunkPos))
                continue;

            //Get the chunk in the defined position
            Chunk newChunk = world.GetChunk(newChunkPos);

            //If the chunk already exists and it's already
            //rendered or in queue to be rendered continue
            if (newChunk != null && newChunk.GetFlag(Chunk.Flag.loadStarted))
                continue;

            for (int y = world.config.minY; y <= world.config.maxY; y += Config.Env.ChunkSize)
                chunksToGenerate.Add(new BlockPos(newChunkPos.x, y, newChunkPos.z));

            return true;
        }

        return false;
    }
}
