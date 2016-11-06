using UnityEngine;
using System.Threading;
using System;
using System.Collections;

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
    public virtual IEnumerator BuildMeshDataCoroutine()
    {
        foreach (BlockPos localBlockPos in new BlockPosEnumerable(Config.Env.ChunkSizePos)) {
            if (chunk.blocks.LocalGet(localBlockPos).Type != 0) {
                if(!chunk.world.UseMultiThreading)
                    Profiler.BeginSample("BuildMeshDataCoroutine.BuildBlock");
                chunk.blocks.LocalGet(localBlockPos).BuildBlock(chunk, localBlockPos, localBlockPos + chunk.pos, meshData);
                if(!chunk.world.UseMultiThreading)
                    Profiler.EndSample();
                yield return null;
            }
        }
        meshData.ConvertToArrays();
    }

    public virtual void BuildMesh()
    {
        meshData.CommitMesh();
    }

}
