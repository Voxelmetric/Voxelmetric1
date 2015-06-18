using UnityEngine;
using System.Collections;

public class BlockController
{
    //Base block constructor
    public BlockController() { }

    public virtual void AddBlockData (Chunk chunk, BlockPos pos, MeshData meshData, Block block) { }

    public virtual void BuildBlock(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {
        PreRender(chunk, pos, block);
        AddBlockData(chunk, pos, meshData, block);
        PostRender(chunk, pos, block);
    }


    public virtual bool IsSolid(Direction direction) { return false; }

    public virtual bool HasLight() { return false; }

    public virtual bool EmitsLight() { return false; }

    public virtual void OnCreate(Chunk chunk, BlockPos pos, Block block) { }

    public virtual void PreRender(Chunk chunk, BlockPos pos, Block block) { }

    public virtual void PostRender(Chunk chunk, BlockPos pos, Block block) { }

    public virtual void OnDestroy(Chunk chunk, BlockPos pos, Block block) { }
}