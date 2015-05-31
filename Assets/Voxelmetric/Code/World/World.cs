using UnityEngine;
using System.Threading;
using System.Collections.Generic;

public class World : MonoBehaviour {

    public Dictionary<BlockPos, Chunk> chunks = new Dictionary<BlockPos, Chunk>();
    public GameObject chunkPrefab;

    public string worldName = "world";

    //Instantiates a chunk at the supplied coordinates using the chunk prefab,
    //then runs terrain generation on it and loads the chunk's save file
    public void CreateChunk(BlockPos pos)
    {
        GameObject newChunkObject = Instantiate(
                        chunkPrefab, pos,
                        Quaternion.Euler(Vector3.zero)
                    ) as GameObject;

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

    void GenAndLoadChunk(Chunk chunk)
    {
        var terrainGen = new TerrainGen();
        terrainGen.ChunkGen(chunk);

        Serialization.Load(chunk);
        chunk.terrainGenerated = true;
    }

    //Saves the chunk and destroys the game object
    public void DestroyChunk(BlockPos pos)
    {
        Chunk chunk = null;
        if (chunks.TryGetValue(pos, out chunk))
        {
            Serialization.SaveChunk(chunk);
            Object.Destroy(chunk.gameObject);
            chunks.Remove(pos);
        }
    }

    //returns the chunk that contains the given block position or null if there is none
    public Chunk GetChunk(BlockPos pos)
    {
        //Get the coordinates of the chunk containing this block
        pos = pos.ContainingChunkCoordinates();

        Chunk containerChunk = null;
        chunks.TryGetValue(pos, out containerChunk);

        return containerChunk;
    }

    //returns the block at the given global coordinates
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
                return Block.Stone;
            }

            return containerChunk.GetBlock(localPos);
        }
        else
        {
            //return a solid block so that the faces beside it aren't rendered
            return Block.Stone;
        }

    }

    //Gets the chunk and sets the block at the given coordinates, updates the chunk and its
    //neighbors if the update chunk flag is true or not set
    public void SetBlock(BlockPos pos, Block block, bool updateChunk = true)
    {
        Chunk chunk = GetChunk(pos);
        if (chunk != null)
        {
            BlockPos localPos = pos - chunk.pos;
            chunk.SetBlock(localPos, block, updateChunk);

            if (updateChunk)
            {
                //Checks to see if the block position is on the border of the chunk 
                //and if so update the chunk it's touching
                UpdateIfEqual(localPos.x, 0                    , pos.Add(-1,0,0));
                UpdateIfEqual(localPos.x, Config.Env.ChunkSize - 1 , pos.Add(1, 0, 0));
                UpdateIfEqual(localPos.y, 0                    , pos.Add(0,-1,0));
                UpdateIfEqual(localPos.y, Config.Env.ChunkSize - 1 , pos.Add(0, 1, 0));
                UpdateIfEqual(localPos.z, 0                    , pos.Add(0, 0, -1));
                UpdateIfEqual(localPos.z, Config.Env.ChunkSize - 1 , pos.Add(0, 0, 1));
            }
        
        }
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
