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

    public virtual string Name() { return "BlockController";  }

    public virtual bool IsSolid(Direction direction) { return false; }

    public virtual bool IsTransparent() { return false; }

    public virtual byte LightEmmitted() { return 0; }

    /// <summary>
    /// Returns true if the block can be used as a possible path for path finding
    /// </summary>
    public virtual bool CanBeWalkedOn(Block block) { return false; }

    /// <summary>
    /// Returns true if the block does not block entities in path finding
    /// </summary>
    public virtual bool CanBeWalkedThrough(Block block) { return true; }

    public virtual void OnCreate(Chunk chunk, BlockPos pos, Block block) { }

    public virtual void PreRender(Chunk chunk, BlockPos pos, Block block) { }

    public virtual void PostRender(Chunk chunk, BlockPos pos, Block block) { }

    public virtual void OnDestroy(Chunk chunk, BlockPos pos, Block block) { }

}