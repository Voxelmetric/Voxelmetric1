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

    ///////////////////////////////////////////
    //        Extending the controller       //
    ///////////////////////////////////////////
    public Hashtable flags = new Hashtable();

    T GetFlag<T>(object key) where T : new()
    {

        if (!flags.ContainsKey(key))
        {
            return new T();
        }
        return (T)flags[key];
    }

    public void SetFlag(object key, object value)
    {
        if (flags.ContainsKey(key))
        {
            flags.Remove(key);
        }

        flags.Add(key, value);
    }

    public virtual T GetFlagOrOverride<T>(Object key, Chunk chunk, BlockPos pos, Block block) where T : new()
    {
        if (BlockOverride.GetBlockOverride(block.type) != null)
        {
            System.Object overridenReturn = BlockOverride.GetBlockOverride(block.type).GetFlagIntercept(key, chunk, pos, block);
            if (overridenReturn != null)
                return (T)overridenReturn;
        }

        return GetFlag<T>(key);
    }

    public virtual Block OnCreate(Chunk chunk, BlockPos pos, Block block)
    {
        if (BlockOverride.GetBlockOverride(block.type) == null)
            return block;

        return BlockOverride.GetBlockOverride(block.type).OnCreate(chunk, pos, block);
    }

    public virtual void PreRender(Chunk chunk, BlockPos pos, Block block)
    {
        if (BlockOverride.GetBlockOverride(block.type) != null)
            BlockOverride.GetBlockOverride(block.type).PreRender(chunk, pos, block);
    }

    public virtual void PostRender(Chunk chunk, BlockPos pos, Block block)
    {
        if (BlockOverride.GetBlockOverride(block.type) != null)
            BlockOverride.GetBlockOverride(block.type).PostRender(chunk, pos, block);
    }

    public virtual void OnDestroy(Chunk chunk, BlockPos pos, Block block)
    {
        if (BlockOverride.GetBlockOverride(block.type) != null)
            BlockOverride.GetBlockOverride(block.type).OnDestroy(chunk, pos, block);
    }

    public virtual void RandomUpdate(Chunk chunk, BlockPos pos, Block block)
    {
        if (BlockOverride.GetBlockOverride(block.type) != null)
            BlockOverride.GetBlockOverride(block.type).RandomUpdate(chunk, pos, block);
    }

    public virtual void ScheduledUpdate(Chunk chunk, BlockPos pos, Block block)
    {
        if (BlockOverride.GetBlockOverride(block.type) != null)
            BlockOverride.GetBlockOverride(block.type).ScheduledUpdate(chunk, pos, block);
    }
}