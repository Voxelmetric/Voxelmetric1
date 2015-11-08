using UnityEngine;
using System.Collections;

public class BlockController
{
    //Base block constructor
    public BlockController() { }

    public virtual void AddBlockData (Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block) { }

    public virtual void BuildBlock(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    {
        PreRender(chunk, localPos, block);
        AddBlockData(chunk, localPos, globalPos, meshData, block);
        PostRender(chunk, localPos, block);
    }

    //This function should set up the controller for the world 
    public virtual void SetUpController(BlockConfig config, World world)
    {

    }

    public virtual string Name() { return "BlockController";  }

    public virtual bool IsSolid(Direction direction) { return false; }

    public virtual bool IsTransparent() { return false; }

    /// <summary>
    /// Must return a number between 0 and 15, 0 for non emitting
    /// </summary>
    public virtual byte LightEmitted() { return 0; }

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
        return GetFlag<T>(key);
    }

    public virtual Block OnCreate(Chunk chunk, BlockPos pos, Block block)
    {
        return block;
    }

    public virtual void PreRender(Chunk chunk, BlockPos pos, Block block)
    {
        
    }

    public virtual void PostRender(Chunk chunk, BlockPos pos, Block block)
    {
        
    }

    public virtual void OnDestroy(Chunk chunk, BlockPos pos, Block block)
    {
        
    }

    public virtual void RandomUpdate(Chunk chunk, BlockPos pos, Block block)
    {
        
    }

    public virtual void ScheduledUpdate(Chunk chunk, BlockPos pos, Block block)
    {
        
    }
}