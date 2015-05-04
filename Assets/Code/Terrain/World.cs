using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class World : MonoBehaviour {

    public Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();
    public GameObject chunkPrefab;

    public string worldName = "world";

    public void Start(){
    }

    public void CreateChunk(int x, int y, int z)
    {
        BlockPos worldPos = new BlockPos(x, y, z);

        //Instantiate the chunk at the coordinates using the chunk prefab
        GameObject newChunkObject = Instantiate(
                        chunkPrefab, new Vector3(x, y, z),
                        Quaternion.Euler(Vector3.zero)
                    ) as GameObject;

        newChunkObject.transform.parent = gameObject.transform;
        newChunkObject.transform.name = "Chunk (" + x + ", " + y + ", " + z + ")";

        Chunk newChunk = newChunkObject.GetComponent<Chunk>();

        newChunk.pos = worldPos;
        newChunk.world = this;

        //Add it to the chunks dictionary with the position as the key
        chunks.Add(worldPos, newChunk);

        var terrainGen = new TerrainGen();
        terrainGen.ChunkGen(newChunk);

        Serialization.Load(newChunk);

        if(y==-64)
            BlockLight.ResetLightChunkColumn(this, newChunk);
    }

    public void DestroyChunk(int x, int y, int z)
    {
        Chunk chunk = null;
        if (chunks.TryGetValue(new BlockPos(x, y, z), out chunk))
        {
            Serialization.SaveChunk(chunk);
            Object.Destroy(chunk.gameObject);
            chunks.Remove(new BlockPos(x, y, z));
        }
    }

    public Chunk GetChunk(int x, int y, int z)
    {
        BlockPos pos = new BlockPos();
        pos.x = Mathf.FloorToInt(x / (float)Config.ChunkSize) * Config.ChunkSize;
        pos.y = Mathf.FloorToInt(y / (float)Config.ChunkSize) * Config.ChunkSize;
        pos.z = Mathf.FloorToInt(z / (float)Config.ChunkSize) * Config.ChunkSize;

        Chunk containerChunk = null;

        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
    }

    public SBlock GetBlock(int x, int y, int z)
    {
        Chunk containerChunk = GetChunk(x, y, z);


        if (containerChunk != null)
        {
            if (x - containerChunk.pos.x < 0 || x - containerChunk.pos.x >= Config.ChunkSize
                || y - containerChunk.pos.y < 0 || y - containerChunk.pos.y >= Config.ChunkSize
                || z - containerChunk.pos.z < 0 || z - containerChunk.pos.z >= Config.ChunkSize)
            {
                //Should really return blockAir to ensure everything is rendered but
                //for testing return a solid block so nothing is rendered and testers notice
                Debug.LogError("Got local coordinates for a global block lookup");
                return new SBlock(BlockType.air);
            }

            SBlock block = containerChunk.GetBlock(new BlockPos(x - containerChunk.pos.x, y - containerChunk.pos.y, z - containerChunk.pos.z));
            return block;
        }
        else
        {
            return new SBlock(BlockType.stone);
        }

    }

    public void SetBlock(int x, int y, int z, SBlock block, bool updateChunk = true)
    {
        Chunk chunk = GetChunk(x, y, z);

        if (chunk != null)
        {
            chunk.SetBlock(x - chunk.pos.x, y - chunk.pos.y, z - chunk.pos.z, block, updateChunk);
            if (updateChunk)
            {
                BlockLight.LightArea(this, new BlockPos(x, y, z));

                UpdateIfEqual(x - chunk.pos.x, 0, new BlockPos(x - 1, y, z));
                UpdateIfEqual(x - chunk.pos.x, Config.ChunkSize - 1, new BlockPos(x + 1, y, z));
                UpdateIfEqual(y - chunk.pos.y, 0, new BlockPos(x, y - 1, z));
                UpdateIfEqual(y - chunk.pos.y, Config.ChunkSize - 1, new BlockPos(x, y + 1, z));
                UpdateIfEqual(z - chunk.pos.z, 0, new BlockPos(x, y, z - 1));
                UpdateIfEqual(z - chunk.pos.z, Config.ChunkSize - 1, new BlockPos(x, y, z + 1));
            }
        
        }
    }

    void UpdateIfEqual(int value1, int value2, BlockPos pos)
    {
        if (value1 == value2)
        {
            Chunk chunk = GetChunk(pos.x, pos.y, pos.z);
            if (chunk != null)
                chunk.update = true;
        }
    }
}
