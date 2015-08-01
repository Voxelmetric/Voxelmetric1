using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using SimplexNoise;

[RequireComponent(typeof(TerrainGen))]
public class World : MonoBehaviour {

    private static World _instance;

    //Lets this class be fetched as a singleton
    public static World instance
    {
        get
        {
            if (_instance == null)
                _instance = GameObject.FindObjectOfType<World>();
            return _instance;
        }
    }

    public Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();
    public GameObject chunkPrefab;
    List<GameObject> chunkPool = new List<GameObject>();

    //This world name is used for the save file name
    public string worldName = "world";
    public Noise noiseGen;
    TerrainGen terrainGen;
    public System.Random random;

    void Start()
    {
        //Makes the block index fetch all the BlockDefinition components
        //on this gameobject and add them to the index
        Block.index.GetMissingDefinitions();
        noiseGen = new Noise(worldName);
        terrainGen = gameObject.GetComponent<TerrainGen>();
        terrainGen.noiseGen = noiseGen;
        terrainGen.world = this;
        random = new System.Random();
    }

    /// <summary>
    ///Instantiates a chunk at the supplied coordinates using the chunk prefab,
    ///then runs terrain generation on it and loads the chunk's save file
    /// </summary>
    /// <param name="pos">The world position to create this chunk.</param>
    public void CreateChunk(BlockPos pos)
    {
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
            newChunkObject.transform.position= pos;
        }

        newChunkObject.transform.parent = gameObject.transform;
        newChunkObject.transform.name = "Chunk (" + pos + ")";

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();

        newChunk.pos = pos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(pos, newChunk);

        if (Config.Toggle.UseMultiThreading) {
            Thread thread = new Thread(() => { GenAndLoadChunk(newChunk); });
            thread.Start();
        }
        else
        {
            GenAndLoadChunk(newChunk);
        }
    }


    /// <summary>
    ///Load terrain, saved changes and resets
    ///the light for an empty chunk
    /// </summary>
    /// <param name="chunk">The chunk to generate and load for</param>
    protected virtual void GenAndLoadChunk(Chunk chunk)
    {
        if (chunk.pos.y == Config.Env.WorldMaxY)
        {
            terrainGen.GenerateTerrainForChunkColumn(chunk.pos);

            for (int i = Config.Env.WorldMinY; i < Config.Env.WorldMaxY; i += Config.Env.ChunkSize)
                Serialization.Load(GetChunk(new BlockPos(chunk.pos.x, i, chunk.pos.z)));

            if (Config.Toggle.LightSceneOnStart)
                BlockLight.ResetLightChunkColumn(this, chunk);
        }

        chunk.SetFlag(Chunk.Flag.terrainGenerated, true);
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
                    Serialization.SaveChunk(chunk);
                    chunk.MarkForDeletion();
                });
                thread.Start();
            }
            else
            {
                if(chunk.GetFlag(Chunk.Flag.chunkModified))
                    Serialization.SaveChunk(chunk);

                chunk.MarkForDeletion();
            }

            chunks.Remove(pos);
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
            BlockPos localPos = pos - containerChunk.pos;
            if (!Chunk.InRange(localPos))
            {
                //This gets called if somehow our function got caught in a loop
                //between World's GetBlock and Chunk's GetBlock
                Debug.LogError("Error while setting block");
                return Block.Void;
            }

            return containerChunk.GetBlock(localPos);
        }
        else
        {
            return "solid";
        }

    }

    /// <summary>
    /// Gets the chunk and sets the block at the given coordinates, updates the chunk and its
    /// neighbors if the update chunk flag is true or not set. Uses global coordinates, to use
    /// local coordinates use the chunk's SetBlock function.
    /// </summary>
    /// <param name="pos">Global position of the block</param>
    /// <param name="block">The block be placed</param>
    /// <param name="updateChunk">Optional parameter, set to false not update the chunk despite the change</param>
    public void SetBlock(BlockPos pos, Block block, bool updateChunk = true)
    {
        Chunk chunk = GetChunk(pos);
        if (chunk != null)
        {
            BlockPos localPos = pos - chunk.pos;
            chunk.SetBlock(localPos, block, updateChunk);

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
                chunk.UpdateChunk();
        }
    }
}
