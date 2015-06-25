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
    }

    void Update()
    {
        if (markedForDeletion)
        {
            Destroy(gameObject);
        }

        if (meshReady)
        {
            meshReady = false;
            RenderMesh();
            meshData = new MeshData();
            busy = false;
        }

    }

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

    //gets the block from the blocks array or gets it from World
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

    public void SetBlock(BlockPos blockPos, Block block, bool updateChunk = true)
    {
        if (InRange(blockPos))
        {
            blocks[blockPos.x, blockPos.y, blockPos.z].controller.OnDestroy(this, pos, blocks[blockPos.x, blockPos.y, blockPos.z]);

            blocks[blockPos.x, blockPos.y, blockPos.z] = block;

            blocks[blockPos.x, blockPos.y, blockPos.z].controller.OnCreate(this, pos, blocks[blockPos.x, blockPos.y, blockPos.z]);

            if (updateChunk)
                UpdateChunk();
        }
        else
        {
            //if the block is out of range set it though world
            world.SetBlock(blockPos + pos, block, updateChunk);
        }
    }

    // Updates the chunk based on its contents
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

    // Sends the calculated mesh information
    // to the mesh and collision components
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

    public void MarkForDeletion()
    {
        markedForDeletion = true;
    }

    public bool IsMarkedForDeletion()
    {
        return markedForDeletion;
    }

}