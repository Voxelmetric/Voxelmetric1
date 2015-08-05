using UnityEngine;
using System.Threading;
using System;
using System.Collections;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{
    private Block[,,] blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];

    private List<BlockAndTimer> scheduledUpdates = new List<BlockAndTimer>();

    public enum Flag {busy, meshReady, loaded, terrainGenerated, markedForDeletion, queuedForUpdate, chunkModified, updateSoon }
    public Hashtable flags = new Hashtable();

    MeshFilter filter;
    MeshCollider coll;

    public World world;
    public BlockPos pos;

    float randomUpdateTime = 0;

    MeshData meshData = new MeshData();

    void Start()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();

        gameObject.GetComponent<Renderer>().material.mainTexture = Block.index.textureIndex.atlas;
    }

    public bool GetFlag(object key)
    {
        if (!flags.ContainsKey(key))
        {
            return false;
        }
        return (bool)flags[key];
    }

    public T GetFlag<T>(object key) where T : new()
    {
        if (!flags.ContainsKey(key))
        {
            return new T();
        }
        return (T)flags[key];
    }

    public void SetFlag(object key, object value)
    {
        if (flags.ContainsKey(key))
        {
            flags.Remove(key);
        }

        flags.Add(key, value);
    }

    void Update()
    {
        

        if (GetFlag(Flag.markedForDeletion) && !GetFlag(Flag.busy))
        {
            ReturnChunkToPool();
        }

        if (GetFlag<bool>(Flag.meshReady))
        {
            SetFlag(Flag.meshReady, false);
            RenderMesh();
            meshData = new MeshData();
            SetFlag(Flag.busy, false);
        }
    }

    void FixedUpdate()
    {
        randomUpdateTime += Time.fixedDeltaTime;

        if (randomUpdateTime >= Config.Env.UpdateFrequency)
        {
            if (GetFlag(Flag.updateSoon))
            {
                UpdateChunk();
                SetFlag(Flag.updateSoon, false);
            }

            randomUpdateTime = 0;

            BlockPos randomPos = new BlockPos();
            randomPos.x = world.random.Next(0, 16);
            randomPos.y = world.random.Next(0, 16);
            randomPos.z = world.random.Next(0, 16);

            GetBlock(randomPos).controller.RandomUpdate(this, randomPos, GetBlock(randomPos));

            ProcessScheduledUpdates();
        }

    }

    void ProcessScheduledUpdates()
    {
        for (int i = 0; i < scheduledUpdates.Count; i++)
        {
            scheduledUpdates[i] = new BlockAndTimer(scheduledUpdates[i].pos, scheduledUpdates[i].time - Config.Env.UpdateFrequency);
            if (scheduledUpdates[i].time <= 0)
            {
                Block block = GetBlock(scheduledUpdates[i].pos);
                block.controller.ScheduledUpdate(this, scheduledUpdates[i].pos, block);
                scheduledUpdates.RemoveAt(i);
                i--;
            }
        }
    }

    public void AddScheduledUpdate(BlockPos pos, float time)
    {
        scheduledUpdates.Add(new BlockAndTimer(pos, time));
    }

    /// <summary>
    /// Updates the chunk either now or as soon as the chunk is no longer busy
    /// </summary>
    public void UpdateChunk()
    {
        if (Config.Toggle.UseMultiThreading)
        {
            Thread thread = new Thread(() =>
            {
                //If there's already an update queued let that one run instead
                if (!GetFlag(Flag.queuedForUpdate))
                {
                    // If the chunk is busy wait for it to be ready, but
                    // set a flag saying an update is waiting so that later
                    // updates don't sit around as well, one is enough
                    if (GetFlag(Flag.busy))
                    {
                        SetFlag(Flag.queuedForUpdate, true);
                        while (GetFlag(Flag.busy))
                        {
                            Thread.Sleep(0);
                        }
                        SetFlag(Flag.queuedForUpdate, false);
                    }

                    SetFlag(Flag.busy, true);
                    BuildMeshData();
                    SetFlag(Flag.meshReady, true);
                }
            });
            thread.Start();
        }
        else //Not using multithreading
        {
            SetFlag(Flag.busy, true);
            BuildMeshData();
            SetFlag(Flag.meshReady, true);
        }
    }

    /// <summary>
    /// Gets and returns a block from a local position within the chunk 
    /// or fetches it from the world
    /// </summary>
    /// <param name="blockPos">A local block position</param>
    /// <returns>The block at the position</returns>
    public Block GetBlock(BlockPos blockPos)
    {
        Block returnBlock;

        if (InRange(blockPos))
        {
            returnBlock = blocks[blockPos.x, blockPos.y, blockPos.z];
        }
        else
        {
            returnBlock = world.GetBlock(blockPos + pos);
        }

        return returnBlock;
    }

    /// <summary>
    /// Returns true if the block local block position is contained in the chunk boundaries
    /// </summary>
    /// <param name="localPos">A local block position</param>
    /// <returns>true or false depending on if the position is in range</returns>
    public static bool InRange(BlockPos localPos)
    {
        if (!InRange(localPos.x))
            return false;
        if (!InRange(localPos.y))
            return false;
        if (!InRange(localPos.z))
            return false;

        return true;
    }

    public static bool InRange(int index)
    {
        if (index < 0 || index >= Config.Env.ChunkSize)
            return false;

        return true;
    }

    /// <summary>
    /// Sets the block at the given local position
    /// </summary>
    /// <param name="blockPos">Local position</param>
    /// <param name="block">Block to place at the given location</param>
    /// <param name="updateChunk">Optional parameter, set to false to keep the chunk unupdated despite the change</param>
    public void SetBlock(BlockPos blockPos, Block block, bool updateChunk = true)
    {
        if (InRange(blockPos))
        {
            //Only call create and destroy if this is a different block type, otherwise it's just updating the properties of an existing block
            if (blocks[blockPos.x, blockPos.y, blockPos.z].type != block.type)
            {
                blocks[blockPos.x, blockPos.y, blockPos.z].controller.OnDestroy(this, blockPos + pos, blocks[blockPos.x, blockPos.y, blockPos.z]);
                block = block.controller.OnCreate(this, blockPos, block);
            }

            blocks[blockPos.x, blockPos.y, blockPos.z] = block;

            if (block.modified)
                SetFlag(Flag.chunkModified, true);

            if (updateChunk)
                UpdateChunk();
        }
        else
        {
            //if the block is out of range set it through world
            world.SetBlock(blockPos + pos, block, updateChunk);
        }
    }

    /// <summary>
    /// Updates the chunk based on its contents
    /// </summary>
    void BuildMeshData()
    {
        for (int x = 0; x < Config.Env.ChunkSize; x++)
        {
            for (int y = 0; y < Config.Env.ChunkSize; y++)
            {
                for (int z = 0; z < Config.Env.ChunkSize; z++)
                {
                    blocks[x, y, z].controller.BuildBlock(this, new BlockPos(x, y, z), meshData, blocks[x,y,z]);
                }
            }
        }
    }

    /// <summary>
    /// Sends the calculated mesh information
    /// to the mesh and collision components
    /// </summary>
    void RenderMesh()
    {
        filter.mesh.Clear();
        filter.mesh.vertices = meshData.vertices.ToArray();
        filter.mesh.triangles = meshData.triangles.ToArray();

        filter.mesh.colors = meshData.colors.ToArray();

        filter.mesh.uv = meshData.uv.ToArray();
        filter.mesh.RecalculateNormals();

        if (Config.Toggle.UseCollisionMesh)
        {
            coll.sharedMesh = null;
            Mesh mesh = new Mesh();
            mesh.vertices = meshData.colVertices.ToArray();
            mesh.triangles = meshData.colTriangles.ToArray();
            mesh.RecalculateNormals();

            coll.sharedMesh = mesh;
        }
    }

    /// <summary>
    /// Marks this chunk for deletion and the next update it will be destroyed
    /// </summary>
    public void MarkForDeletion()
    {
        SetFlag(Flag.markedForDeletion, true);
    }

    public bool IsMarkedForDeletion()
    {
        return GetFlag(Flag.markedForDeletion); ;
    }

    void ReturnChunkToPool()
    {
        flags.Clear();

        if (filter.mesh)
            filter.mesh.Clear();

        if (coll.sharedMesh)
            coll.sharedMesh.Clear();

        blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];
        meshData = new MeshData();

        world.AddToChunkPool(gameObject);
    }

}