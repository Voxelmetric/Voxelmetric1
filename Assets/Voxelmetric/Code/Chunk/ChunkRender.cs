using UnityEngine;
using System.Threading;
using System;
using System.Collections;
using System.Diagnostics;

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
        long maxTime = 5; // Limit to 5 milliseconds
        int numDone = 0, numCheck = 64;
        Stopwatch stopwatch = Stopwatch.StartNew();
        foreach (BlockPos localBlockPos in new BlockPosEnumerable(Config.Env.ChunkSizePos)) {
            if (chunk.blocks.LocalGet(localBlockPos).type != 0) {
                chunk.blocks.LocalGet(localBlockPos).BuildBlock(chunk, localBlockPos, localBlockPos + chunk.pos, meshData);
                ++numDone;
                if (numDone % numCheck == 0) {
                    if (stopwatch.ElapsedMilliseconds >= maxTime) {
                        stopwatch.Reset();
                        yield return null;
                        stopwatch.Start();
                    }
                }
            }
        }
        meshData.ConvertToArrays();
        yield return null;
    }

    public virtual void BuildMesh()
    {
        meshData.CommitMesh();
    }

}
