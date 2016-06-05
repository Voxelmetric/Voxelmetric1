using UnityEngine;

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

    public ChunkRender(Chunk chunk)
    {
        this.chunk = chunk;
    }


    /// <summary> Updates the chunk based on its contents </summary>
    public virtual void BuildMeshData()
    {
        foreach (BlockPos localBlockPos in new BlockPosEnumerable(Config.Env.ChunkSizePos))
        {
            if (chunk.blocks.LocalGet(localBlockPos).type == 0)
                continue;

            chunk.blocks.LocalGet(localBlockPos).BuildBlock(chunk, localBlockPos, localBlockPos + chunk.pos, meshData);
        }
        meshData.ConvertToArrays();
    }

    public virtual void BuildMesh()
    {
        meshData.CommitMesh();
    }

}
