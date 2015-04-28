using UnityEngine;
using System.Collections;

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]

public class Chunk : MonoBehaviour
{

    public SBlock[, ,] blocks = new SBlock[Config.ChunkSize, Config.ChunkSize, Config.ChunkSize];

    public bool update = false;

    MeshFilter filter;
    MeshCollider coll;

    public World world;
    public BlockPos pos;

    public bool rendered;

    void Start()
    {
        filter = gameObject.GetComponent<MeshFilter>();
        coll = gameObject.GetComponent<MeshCollider>();
    }

    //Update is called once per frame
    void Update()
    {
        if (update)
        {
            update = false;
            UpdateChunk();
        }
    }

    public SBlock GetBlock(BlockPos blockPos)
    {
        if (InRange(blockPos.x) && InRange(blockPos.y) && InRange(blockPos.z))
            return blocks[blockPos.x, blockPos.y, blockPos.z];

        return world.GetBlock(pos.x + blockPos.x, pos.y + blockPos.y, pos.z + blockPos.z);
    }

    public static bool InRange(int index)
    {
        if (index < 0 || index >= Config.ChunkSize)
            return false;

        return true;
    }

    public void SetBlock(int x, int y, int z, SBlock block, bool updateChunk = true)
    {
        if (InRange(x) && InRange(y) && InRange(z))
        {
            blocks[x, y, z] = block;
            if (updateChunk)
                update = true;
        }
        else
        {
            world.SetBlock(pos.x + x, pos.y + y, pos.z + z, block, updateChunk);
        }

    }

    // Updates the chunk based on its contents
    public bool UpdateChunk()
    {
        rendered = true;
        
        //if(pos.y==48)
            //BlockLight.LightArea(world, pos.Add(8,8,8));

        MeshData meshData = new MeshData();

        for (int x = 0; x < Config.ChunkSize; x++)
        {
            for (int y = 0; y < Config.ChunkSize; y++)
            {
                for (int z = 0; z < Config.ChunkSize; z++)
                {
                    blocks[x, y, z].BuildBlock(this, new BlockPos(x, y, z), meshData);
                }
            }
        }

        RenderMesh(meshData);

        return true;
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

        Profiler.BeginSample("collision mesh");
        coll.sharedMesh = null;
        Mesh mesh = new Mesh();
        mesh.vertices = meshData.colVertices.ToArray();
        mesh.triangles = meshData.colTriangles.ToArray();
        mesh.RecalculateNormals();

        coll.sharedMesh = mesh;
        Profiler.EndSample();
    }

}