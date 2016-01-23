using UnityEngine;
using System.Threading;
using System;

public class ChunkRender {

    protected MeshData meshData = new MeshData();
    protected Chunk chunk;

    public Mesh mesh
    {
        get
        {
            return meshData.mesh;
        }
    }

    public bool needsUpdate;

    public ChunkRender(Chunk chunk)
    {
        this.chunk = chunk;
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
        meshData.ConvertToArrays();
    }

    public virtual void BuildMesh()
    {
        meshData.CommitMesh();
    }

}
