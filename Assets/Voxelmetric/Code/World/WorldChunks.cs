using UnityEngine;
using System.Collections.Generic;

public class WorldChunks {

    World world;

    Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();
    public Dictionary<BlockPos, Chunk>.ValueCollection chunkCollection { get { return chunks.Values; } }
    public Dictionary<BlockPos, Chunk>.KeyCollection posCollection { get { return chunks.Keys; } }

    List<GameObject> chunkPool = new List<GameObject>();
    GameObject chunkPrefab;

    public WorldChunks(World world) {
        this.world = world;
        chunkPrefab = Resources.Load<GameObject>(world.config.pathToChunkPrefab);
    }

    /// <summary> Updates chunks and fires frequent events for chunks. Called by world's LateUpdate</summary>
    public void ChunksUpdate()
    {
        foreach (var chunk in chunkCollection)
        {
            chunk.RegularUpdate();
        }
    }

    /// <summary> Returns the chunk at the given position </summary>
    /// <param name="pos">Position of the chunk or of a block within the chunk</param>
    /// <returns>The chunk that contains the given block position or null if there is none</returns>
    public Chunk Get(BlockPos pos) {
        pos = pos.ContainingChunkCoordinates();

        Chunk containerChunk = null;
        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
    }

    /// <summary> Adds the given block at this position if there isn't already one </summary>
    /// <param name="pos">The coordinates of the chunk or coordinates within the chunk</param>
    /// <param name="chunk">The chunk to add</param>
    public void Add(BlockPos pos, Chunk chunk)
    {
        pos = pos.ContainingChunkCoordinates();
        if (chunks.ContainsKey(pos))
            return;

        chunks.Add(pos, chunk);
    }

    public void Remove(BlockPos pos)
    {
        chunks.Remove(pos);
    }

    Chunk New(BlockPos pos)
    {
        Chunk existingChunk = Get(pos);
        if (existingChunk != null)
            return existingChunk;

        GameObject newChunkObject;
        if (chunkPool.Count == 0)
        {
            //No chunks in pool, create new
            newChunkObject = Object.Instantiate(
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

        newChunkObject.transform.parent = world.gameObject.transform;
        newChunkObject.transform.name = "Chunk (" + pos + ")";

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();

        newChunk.pos = pos;
        newChunk.world = world;

        newChunk.Start();

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(pos, newChunk);

        return newChunk;
    }


    /// <summary> Instantiates a chunk and chunks at all positions surrounding it </summary>
    /// <param name="pos">The world position to create this chunk.</param>
    /// <returns>The chunk created in the center<returns>
    public Chunk CreateChunkAndNeighbors(BlockPos pos)
    {
        pos = pos.ContainingChunkCoordinates();

        //Create neighbors
        for (int x = -1; x <= 1; x++)
            for (int z = -1; z <= 1; z++)
                for (int y = -1; y <= 1; y++)
                    if (y >= world.config.minY)
                        New(pos.Add(x * Config.Env.ChunkSize, y * Config.Env.ChunkSize, z * Config.Env.ChunkSize));

        Chunk newChunk = Get(pos);
        newChunk.StartLoading();

        return newChunk;
    }

    public void AddToChunkPool(GameObject chunk)
    {
        chunk.SetActive(false);
        chunkPool.Add(chunk);
    }

    /// <summary>
    /// Updates any chunks neighboring a block position
    /// </summary>
    /// <param name="pos">position of change</param>
    public void UpdateAdjacent(BlockPos pos)
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
            Chunk chunk = Get(pos);
            if (chunk != null)
                chunk.UpdateNow();
        }
    }
}
