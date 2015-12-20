using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using SimplexNoise;

public class World : MonoBehaviour {
    public readonly Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();
    List<Chunk> chunksToDelete = new List<Chunk>();
    List<GameObject> chunkPool = new List<GameObject>();

    public string worldConfig;

    public WorldConfig config;
    public BlockIndex blockIndex;
    public TextureIndex textureIndex;

    //This world name is used for the save file name and as a seed for random noise
    // leave empty to override with 
    public string worldName = "world";

    TerrainGen terrainGen;
    TerrainLayer[] terrainLayers;
    GameObject chunkPrefab;
    public Noise noise;
    
    [HideInInspector]
    public int worldIndex;

    void Start()
    {
        config = new ConfigLoader<WorldConfig>(new string[] {"Worlds"}).GetConfig(worldConfig);
        noise = new Noise(worldName);

        worldIndex = Voxelmetric.resources.worlds.Count;
        Voxelmetric.resources.AddWorld(this);

        textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
        blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);

        chunkPrefab = Resources.Load<GameObject>(config.pathToChunkPrefab);
    }

    void LateUpdate()
    {
        for (int i = 0; i < chunksToDelete.Count; i++)
        {
            if (chunksToDelete[i] != null)
                chunksToDelete[i].ReturnChunkToPool();
        }

        chunksToDelete.Clear();
    }

    public Chunk CreateChunk(BlockPos pos)
    {
        Chunk existingChunk = GetChunk(pos);
        if (existingChunk != null)
        {
            return existingChunk;
        }

        GameObject newChunkObject;
        if (chunkPool.Count == 0)
        {
            //No chunks in pool, create new
            newChunkObject = Instantiate(
                            chunkPrefab, pos,
                            Quaternion.Euler(Vector3.zero)
                        ) as GameObject;
        }
        else
        {
            //Load a chunk from the pool
            newChunkObject = chunkPool[0];
            chunkPool.RemoveAt(0);
            newChunkObject.SetActive(true);
            newChunkObject.transform.position = pos;
        }

        newChunkObject.transform.parent = gameObject.transform;
        newChunkObject.transform.name = "Chunk (" + pos + ")";

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();

        newChunk.pos = pos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(pos, newChunk);
        return newChunk;
    }

    /// <summary>
    ///Instantiates a chunk at the supplied coordinates using the chunk prefab,
    ///then runs terrain generation on it and loads the chunk's save file
    /// </summary>
    /// <param name="pos">The world position to create this chunk.</param>
    public Chunk CreateChunkAndNeighbors(BlockPos pos)
    {
        pos = pos.ContainingChunkCoordinates();
        Chunk newChunk = CreateChunk(pos);

        if (newChunk.GetFlag(Chunk.Flag.loadStarted))
            return newChunk;

        newChunk.SetFlag(Chunk.Flag.loadStarted, true);

        //Create neighbors
        for (int x = pos.x - Config.Env.ChunkSize; x <= pos.x + Config.Env.ChunkSize; x += Config.Env.ChunkSize)
        {
            for (int z = pos.z - Config.Env.ChunkSize; z <= pos.z + Config.Env.ChunkSize; z += Config.Env.ChunkSize)
            {
                for (int y = pos.y - Config.Env.ChunkSize; y <= pos.y + Config.Env.ChunkSize; y += Config.Env.ChunkSize)
                {
                    if(y>=config.minY)
                    CreateChunk(new BlockPos(x, y, z));
                }
            }
        }

        return newChunk;
    }

    /// <summary>
    /// Saves the chunk and destroys the game object
    /// </summary>
    /// <param name="pos">Position of the chunk to destroy</param>
    public void DestroyChunk(BlockPos pos)
    {
        Chunk chunk = null;
        if (chunks.TryGetValue(pos, out chunk))
        {
            if (Config.Toggle.UseMultiThreading)
            {
                Thread thread = new Thread(() => {
                if(chunk.GetFlag(Chunk.Flag.chunkModified))
                    Serialization.SaveChunk(chunk);

                chunksToDelete.Add(chunk);
                });
                thread.Start();
            }
            else
            {
                if(chunk.GetFlag(Chunk.Flag.chunkModified))
                    Serialization.SaveChunk(chunk);

                chunksToDelete.Add(chunk);
            }
        }
    }

    public void AddToChunkPool(GameObject chunk)
    {
        chunk.SetActive(false);
        chunkPool.Add(chunk);
    }

    /// <summary>
    /// Get's the chunk object at pos
    /// </summary>
    /// <param name="pos">Position of the chunk or of a block within the chunk</param>
    /// <returns>chunk that contains the given block position or null if there is none</returns>
    public Chunk GetChunk(BlockPos pos)
    {
        //Get the coordinates of the chunk containing this block
        pos = pos.ContainingChunkCoordinates();

        Chunk containerChunk = null;
        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
    }

    /// <summary>
    /// Gets the block at pos
    /// </summary>
    /// <param name="pos">Global position of the block</param>
    /// <returns>The block at the given global coordinates</returns>
    public Block GetBlock(BlockPos pos)
    {
        Chunk containerChunk = GetChunk(pos);

        if (containerChunk != null)
        {
            return containerChunk.GetBlock(pos);
        }
        else
        {
            if (pos.y < config.minY)
            {
                return Block.Solid;
            }
            else
            {
                return Block.Air;
            }
        }
        
    }

    public void SetBlock(BlockPos pos, string block, bool updateChunk = true, bool setBlockModified = true)
    {
        SetBlock(pos, Block.New(block, this), updateChunk, setBlockModified);
    }

    /// <summary>
    /// Gets the chunk and sets the block at the given coordinates, updates the chunk and its
    /// neighbors if the update chunk flag is true or not set. Uses global coordinates, to use
    /// local coordinates use the chunk's SetBlock function.
    /// </summary>
    /// <param name="pos">Global position of the block</param>
    /// <param name="block">The block be placed</param>
    /// <param name="updateChunk">Optional parameter, set to false not update the chunk despite the change</param>
    public void SetBlock(BlockPos pos, Block block, bool updateChunk = true, bool setBlockModified = true)
    {
        Chunk chunk = GetChunk(pos);

        if (chunk != null)
        {
            chunk.SetBlock(pos, block, updateChunk, setBlockModified);

            if (updateChunk)
            {
                UpdateAdjacentChunks(pos);
            }
        }
    }

    /// <summary>
    /// Updates any chunks neighboring a block position
    /// </summary>
    /// <param name="pos">position of change</param>
    public void UpdateAdjacentChunks(BlockPos pos)
    {
        //localPos is the position relative to the chunk's position
        BlockPos localPos = pos - pos.ContainingChunkCoordinates();

        //Checks to see if the block position is on the border of the chunk 
        //and if so update the chunk it's touching
        UpdateIfEqual(localPos.x, 0, pos.Add(-1, 0, 0));
        UpdateIfEqual(localPos.x, Config.Env.ChunkSize - 1, pos.Add(1, 0, 0));
        UpdateIfEqual(localPos.y, 0, pos.Add(0, -1, 0));
        UpdateIfEqual(localPos.y, Config.Env.ChunkSize - 1, pos.Add(0, 1, 0));
        UpdateIfEqual(localPos.z, 0, pos.Add(0, 0, -1));
        UpdateIfEqual(localPos.z, Config.Env.ChunkSize - 1, pos.Add(0, 0, 1));
    }

    void UpdateIfEqual(int value1, int value2, BlockPos pos)
    {
        if (value1 == value2)
        {
            Chunk chunk = GetChunk(pos);
            if (chunk != null)
                chunk.UpdateNow();
        }
    }
}
