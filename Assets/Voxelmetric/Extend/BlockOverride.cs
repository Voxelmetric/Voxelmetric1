using UnityEngine;
using System;

public class BlockOverride {

    public BlockController controller;

    public virtual Block OnCreate(Chunk chunk, BlockPos pos, Block block)
    {
        return block;
    }

    public virtual void OnDestroy(Chunk chunk, BlockPos pos, Block block)
    {

    }

    public virtual void PreRender(Chunk chunk, BlockPos pos, Block block)
    {

    }

    public virtual void PostRender(Chunk chunk, BlockPos pos, Block block)
    {

    }

    public virtual void RandomUpdate(Chunk chunk, BlockPos pos, Block block)
    {

    }

    public virtual void ScheduledUpdate(Chunk chunk, BlockPos pos, Block block)
    {

    }

    public static BlockOverride GetBlockOverride(int blockType)
    {
        return Block.index.blockOverrides[blockType];
    }

    public virtual System.Object GetFlagIntercept(System.Object key, Chunk chunk, BlockPos pos, Block block)
    {
        return null; //returning null lets the controller fetch from the flags instead
    }
}
