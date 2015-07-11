using UnityEngine;
using System.Threading;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{
    private Block[,,] blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];

    bool meshReady = false;
    public bool busy = false;
    public bool loaded = false;
    public bool terrainGenerated = false;
    bool markedForDeletion = false;
    bool queuedForUpdate = false;

    MeshFilter filter;
    MeshCollider coll;

    public World world;
    public BlockPos pos;

    MeshData meshData = new MeshData();

    void Start()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();

        gameObject.GetComponent<Renderer>().material.mainTexture = Block.index.textureIndex.atlas;
    }

    void Update()
    {
        if (markedForDeletion && !busy)
        {
            ReturnChunkToPool();
        }

        if (meshReady)
        {
            meshReady = false;
            RenderMesh();
            meshData = new MeshData();
            busy = false;
        }

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
                if (!queuedForUpdate)
                {
                    // If the chunk is busy wait for it to be ready, but
                    // set a flag saying an update is waiting so that later
                    // updates don't sit around as well, one is enough
                    if (busy)
                    {
                        queuedForUpdate = true;
                        while (busy)
                        {
                            Thread.Sleep(0);
                        }
                        queuedForUpdate = false;
                    }

                    busy = true;
                    BuildMeshData();
                    meshReady = true;
                }
            });
            thread.Start();
        }
        else //Not using multithreading
        {
            busy = true;
            BuildMeshData();
            meshReady = true;
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
            blocks[blockPos.x, blockPos.y, blockPos.z].controller.OnDestroy(this, blockPos + pos, blocks[blockPos.x, blockPos.y, blockPos.z]);

            blocks[blockPos.x, blockPos.y, blockPos.z] = block;

            blocks[blockPos.x, blockPos.y, blockPos.z].controller.OnCreate(this, blockPos + pos, blocks[blockPos.x, blockPos.y, blockPos.z]);

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
        markedForDeletion = true;
    }

    public bool IsMarkedForDeletion()
    {
        return markedForDeletion;
    }

    void ReturnChunkToPool()
    {
        meshReady = false;
        busy = false;
        loaded = false;
        terrainGenerated = false;
        markedForDeletion = false;
        queuedForUpdate = false;

        if (filter.mesh)
            filter.mesh.Clear();

        if (coll.sharedMesh)
            coll.sharedMesh.Clear();

        blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];
        meshData = new MeshData();

        world.AddToChunkPool(gameObject);
    }

}