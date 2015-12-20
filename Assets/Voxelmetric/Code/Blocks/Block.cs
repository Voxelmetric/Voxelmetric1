using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public class Block
{
    public Block() { }
    public Block(int type)
    {
        this.type = (ushort)type;
    }

    public ushort type;

    public virtual string       name                { get { return config.name;                     } }
    public virtual string       displayName         { get { return name;                            } }
    public virtual World        world               { get { return Voxelmetric.resources.worlds[0]; } }
    public virtual BlockConfig  config              { get { return world.blockIndex.configs[type];  } }
    public virtual bool         solid               { get { return true;                            } }
    public virtual bool         transparent         { get { return false;                           } }
    public virtual bool         canBeWalkedOn       { get { return true;                            } }
    public virtual bool         canBeWalkedThrough  { get { return false;                           } }

    public virtual bool IsSolid(Direction direction)
    {
        return true;
    }

    public virtual void BuildBlock(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    {
        PreRender(chunk, localPos, globalPos);
        AddBlockData(chunk, localPos, globalPos, meshData);
        PostRender(chunk, localPos, globalPos);
    }

    public virtual void AddBlockData    (Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData) { }
    public virtual void OnCreate        (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void PreRender       (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void PostRender      (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void OnDestroy       (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void RandomUpdate    (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void ScheduledUpdate (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }

    public void SetWorld(int index)
    {

    }

    public static Block New(string name, World world)
    {
        return New(world.blockIndex.GetType(name), world);
    }

    public static Block New(int type, World world)
    {
        return (Block)Activator.CreateInstance(world.blockIndex.GetConfig(type).blockClass);
    }

    public override string ToString()
    {
        return name;
    }

    // Static block types: These are always added as 0 and 1 in the block index
    public static Block Air { get { return new Block(0); } }
    public static Block Solid { get { return new Block(1); } }
}