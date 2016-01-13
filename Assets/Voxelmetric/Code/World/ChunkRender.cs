using UnityEngine;
using System.Threading;

public class ChunkRender {

    protected MeshData meshData = new MeshData();
    protected MeshFilter filter;
    protected MeshCollider coll;

    protected Chunk chunk;

    public ChunkRender(Chunk chunk)
    {
        this.chunk = chunk;

        filter = chunk.gameObject.GetComponent<MeshFilter>();
        coll = chunk.gameObject.GetComponent<MeshCollider>();

        chunk.gameObject.GetComponent<Renderer>().material.mainTexture = chunk.world.textureIndex.atlas;
    }

    /// <summary> Immediately updates the chunk and prepares a mesh to render.
    /// Usually better to use UpdateNow or UpdateSoon </summary>
    public virtual void UpdateChunk()
    {
        chunk.logic.SetFlag(Flag.loadComplete, true);
        if (Config.Toggle.UseMultiThreading)
        {
            // If the chunk is busy set the flag to update it again
            // at the end of the the nearest frame 
            if (chunk.logic.GetFlag(Flag.busy))
            {
                chunk.UpdateNow();
            }
            else
            {
                Thread thread = new Thread(() =>
                {
                    chunk.logic.SetFlag(Flag.busy, true);
                    BuildMeshData();
                    chunk.logic.SetFlag(Flag.meshReady, true);
                });
                thread.Start();
            }
        }
        else //Not using multithreading
        {
            chunk.logic.SetFlag(Flag.busy, true);
            BuildMeshData();
            chunk.logic.SetFlag(Flag.meshReady, true);
        }
    }


    /// <summary> Updates the chunk based on its contents </summary>
    public virtual void BuildMeshData()
    {
        for (int x = 0; x < Config.Env.ChunkSize; x++)
        {
            for (int y = 0; y < Config.Env.ChunkSize; y++)
            {
                for (int z = 0; z < Config.Env.ChunkSize; z++)
                {
                    BlockPos localBlockPos = new BlockPos(x, y, z);
                    if (chunk.blocks.LocalGet(localBlockPos).type != 0)
                    {
                        chunk.blocks.LocalGet(localBlockPos).BuildBlock(chunk, localBlockPos, localBlockPos + chunk.pos, meshData);
                    }
                }
            }
        }
    }

    /// <summary> Sends the calculated mesh information
    /// to the mesh and collision components </summary>
    public virtual void RenderMesh()
    {
        filter.mesh.Clear();
        filter.mesh.vertices = meshData.vertices.ToArray();
        filter.mesh.triangles = meshData.triangles.ToArray();

        filter.mesh.colors = meshData.colors.ToArray();

        filter.mesh.uv = meshData.uv.ToArray();
        filter.mesh.RecalculateNormals();

        if (chunk.world.config.useCollisionMesh)
        {
            coll.sharedMesh = null;
            Mesh mesh = new Mesh();
            mesh.vertices = meshData.colVertices.ToArray();
            mesh.triangles = meshData.colTriangles.ToArray();
            mesh.RecalculateNormals();

            coll.sharedMesh = mesh;
        }
    }

    public void ResetContent()
    {
        ClearMeshData();

        if (filter.mesh)
            filter.mesh.Clear();

        if (coll.sharedMesh)
            coll.sharedMesh.Clear();
    }

    public void ClearMeshData() {
        meshData.ClearMesh();
    }

}
