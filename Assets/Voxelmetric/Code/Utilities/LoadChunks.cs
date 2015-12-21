using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class LoadChunks : MonoBehaviour
{

    public World world;
    public bool generateTerrain = true;
    TerrainGen terrainGen;
    public string layerFolder;
    private BlockPos objectPos;

    [Range(1, 64)]
    public int chunkLoadRadius = 8;
    BlockPos[] chunkPositions;

    //The distance is measured in chunks
    [Range(1, 64)]
    public int DistanceToDeleteChunks = (int)(8 * 1.25f);
    private int distanceToDeleteInUnitsSquared;

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
    List<BlockPos> chunksToRender = new List<BlockPos>();

    int deleteTimer = 0;

    List<BlockPos> chunksToGenerate = new List<BlockPos>();

    void Start()
    {
        chunkPositions = ChunkLoadOrder.ChunkPositions(chunkLoadRadius);
        distanceToDeleteInUnitsSquared = (int)(DistanceToDeleteChunks * Config.Env.ChunkSize * Config.Env.BlockSize);
        distanceToDeleteInUnitsSquared *= distanceToDeleteInUnitsSquared;
    }

    // Update is called once per frame
    void Update()
    {
        objectPos = transform.position;

        if (generateTerrain && terrainGen == null)
        {
            //Cant load chunks until the world is started so we can initialize TerrainGen
            if (world.noise != null)
            {
                terrainGen = new TerrainGen(world, layerFolder);
            }
            else
            {
                return;
            }
        }

        if (deleteTimer == WaitBetweenDeletes)
        {
            if (Config.Toggle.UseMultiThreading)
            {
                Thread thread = new Thread(() =>
                {
                    DeleteChunks();
                });
                thread.Start();
            }
            else
            {
                DeleteChunks();
            }

            deleteTimer = 0;
            return;
        }
        else
        {
            deleteTimer++;
        }

        if (chunksToRender.Count != 0)
        {
            for (int i = 0; i < ChunksToLoadPerFrame; i++)
            {
                if (chunksToRender.Count == 0)
                {
                    break;
                }

                BlockPos pos = chunksToRender[0];
                world.GetChunk(pos).UpdateChunk();
                chunksToRender.RemoveAt(0);
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
            Chunk newChunk = world.CreateChunkAndNeighbors(pos);

            if (Config.Toggle.UseMultiThreading)
            {
                Thread thread = new Thread(() => { GenAndLoadChunk(newChunk); });
                thread.Start();
            }
            else
            {
                GenAndLoadChunk(newChunk);
            }

            chunksToGenerate.RemoveAt(0);
        }

    }

    void DeleteChunks()
    {
        int posX = objectPos.x;
        int posZ = objectPos.z;

        var chunksToDelete = new List<BlockPos>();
        foreach (var chunk in world.chunks)
        {
            BlockPos chunkPos = chunk.Key;
            int xd = posX - chunkPos.x;
            int yd = posZ - chunkPos.z;

            if ((xd * xd + yd * yd) > distanceToDeleteInUnitsSquared)
            {
                chunksToDelete.Add(chunk.Key);
            }
        }

        for(int i = 0; i< chunksToDelete.Count; i++)// (var chunk in chunksToDelete)
        {
            world.DestroyChunk(chunksToDelete[i]);
        }
    }

    bool FindChunksAndLoad()
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

    /// <summary>
    ///Load terrain, saved changes and resets
    ///the light for an empty chunk
    /// </summary>
    /// <param name="chunk">The chunk to generate and load for</param>
    protected virtual void GenAndLoadChunk(Chunk chunk)
    {
        WorldConfig config = world.config;

        if (chunk.pos.y == config.maxY)
        {
            for (int x = chunk.pos.x - Config.Env.ChunkSize; x <= chunk.pos.x + Config.Env.ChunkSize; x += Config.Env.ChunkSize)
            {
                for (int z = chunk.pos.z - Config.Env.ChunkSize; z <= chunk.pos.z + Config.Env.ChunkSize; z += Config.Env.ChunkSize)
                {
                    Chunk genChunk = world.GetChunk(new BlockPos(x, config.maxY, z));

                    if (!genChunk.GetFlag(Chunk.Flag.contentsGenerated) && !genChunk.GetFlag(Chunk.Flag.generationInProgress))
                    {
                        genChunk.SetFlag(Chunk.Flag.generationInProgress, true);
                        terrainGen.GenerateTerrainForChunkColumn(new BlockPos(x, config.maxY, z));

                        for (int i = config.minY; i <= config.maxY; i += Config.Env.ChunkSize)
                            Serialization.Load(world.GetChunk(new BlockPos(chunk.pos.x, i, chunk.pos.z)));

                        for (int y = config.minY; y <= config.maxY; y += Config.Env.ChunkSize)
                            world.GetChunk(new BlockPos(x, y, z)).SetFlag(Chunk.Flag.contentsGenerated, true);
                    }
                    else if (Config.Toggle.UseMultiThreading)
                    {
                        for (int y = config.minY; y <= config.maxY; y += Config.Env.ChunkSize)
                        {
                            while (!world.GetChunk(new BlockPos(x, y, z)).GetFlag(Chunk.Flag.contentsGenerated))
                            {
                                Thread.Sleep(0);
                            }
                        }
                    }

                }
            }
        }
        else
        {
            //Chunk generated was not the top one so the column isn't complete yet, return without rendering
            return;
        }

        for (int i = config.minY; i <= config.maxY; i += Config.Env.ChunkSize)
        {
            chunksToRender.Add(new BlockPos(chunk.pos.x, i, chunk.pos.z));
        }
    }
}
