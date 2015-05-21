using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{

    public Block[, ,] blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];

    bool update = false;

    MeshFilter filter;
    MeshCollider coll;

    public World world;
    public BlockPos pos;

    public bool rendered = false;

    void Start()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();
    }

    void Update()
    {
        if (update)
        {
            update = false;
            UpdateChunk();
        }
    }

    public void QueueUpdate()
    {
        update = true;
    }

    //gets the block from the blocks array or gets it from World
    public Block GetBlock(BlockPos blockPos)
    {
        if (InRange(blockPos))
            return blocks[blockPos.x, blockPos.y, blockPos.z];

        return world.GetBlock(blockPos + pos);
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
            blocks[blockPos.x, blockPos.y, blockPos.z] = block;

            if (updateChunk)
                update = true;
        }
        else
        {
            //if the block is out of range set it though world
            world.SetBlock(blockPos + pos, block, updateChunk);
        }
    }

    // Updates the chunk based on its contents
    void UpdateChunk()
    {
        rendered = true;
        MeshData meshData = new MeshData();

        for (int x = 0; x < Config.Env.ChunkSize; x++)
        {
            for (int y = 0; y < Config.Env.ChunkSize; y++)
            {
                for (int z = 0; z < Config.Env.ChunkSize; z++)
                {
                    //TODO: blocks should have a controller parameter which would have this build block function
                    blocks[x, y, z].controller.BuildBlock(this, new BlockPos(x, y, z), meshData);
                }
            }
        }

        RenderMesh(meshData);
    }

    // Sends the calculated mesh information
    // to the mesh and collision components
    void RenderMesh(MeshData meshData)
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

}