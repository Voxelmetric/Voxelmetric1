using System;
using System.Runtime.InteropServices;
using UnityEngine;

[Serializable]
public class Block
{
    // Static block types: These are always added as 0 and 1 in the block index
    public static readonly int VoidType = 0;
    public static readonly int AirType = 1;

    public Block() { }
    public Block(int type)
    {
        this.type = (ushort)type;
    }

    public ushort type;
    public byte worldIndex;

    [NonSerialized()] private BlockConfig cachedConfig;
    [NonSerialized()] public World world;

    public virtual string       name                { get { return config.name;                     } }
    public virtual string       displayName         { get { return name;                            } }
    public virtual BlockConfig  config              { get {
            if (cachedConfig == null || cachedConfig.type != type)
                cachedConfig = world.blockIndex.configs[type];
            return cachedConfig;
        }
    }
    public virtual bool         solid               { get { return config.solid;                    } }
    public virtual bool         canBeWalkedOn       { get { return config.canBeWalkedOn;            } }
    public virtual bool         canBeWalkedThrough  { get { return config.canBeWalkedThrough;       } }

    public virtual bool IsSolid(Direction direction)
    {
        return solid;
    }

    public virtual void BuildBlock(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
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
    public virtual bool RaycastHit(Vector3 pos, Vector3 dir, BlockPos bPos) { return solid; } 

    public static Block Create(string name, World world)
    {
        return Create(world.blockIndex.GetType(name), world);
    }

    public static Block Create(int type, World world)
    {
        Block block = (Block)Activator.CreateInstance(world.blockIndex.GetConfig(type).blockClass);
        block.type = (ushort)type;
        block.world = world;
        return block;
    }

    public override string ToString()
    {
        return name;
    }

    public override bool Equals(object obj) { return obj != null && GetHashCode() == ((Block)obj).GetHashCode(); }

    //Override this to create a hash of the block type and the block's data
    public override int GetHashCode() { return type * 227; }

    public int RestoreBlockData(byte[] data, int offset)
    {
        return 0;
    }

    public byte[] ToByteArray()
    {
        return BitConverter.GetBytes(type);
    }
}