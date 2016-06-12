using System;
using System.Runtime.Serialization;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

[Serializable]
public class Block: ISerializable
{
    public ushort type;
    public BlockConfig config { get; set; }

    public Block()
    {
        type = 0;
        config = null;
    }

    public Block(int type, BlockConfig config)
    {
        Assert.IsTrue(config != null);
        this.type = (ushort)type;
        this.config = config;
    }

    // Deserialization
    public Block(SerializationInfo info, StreamingContext text) : this()
    {
        World world = text.Context as World;
        type = info.GetUInt16("type");
        config = world.blockIndex.GetConfig(type);
    }

    // Serialization
    public void GetObjectData(SerializationInfo info, StreamingContext context)
    {
        info.AddValue("type", type);
    }

    public virtual string       name                { get { return config.name;                     } }
    public virtual string       displayName         { get { return name;                            } }
    public virtual bool         solid               { get { return config.solid;                    } }
    public virtual bool         canBeWalkedOn       { get { return config.canBeWalkedOn;            } }
    public virtual bool         canBeWalkedThrough  { get { return config.canBeWalkedThrough;       } }

    public virtual bool IsSolid(Direction direction)
    {
        return solid;
    }

    public virtual void BuildBlock(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        PreRender(chunk, localPos, globalPos);
        AddBlockData(chunk, localPos, globalPos);
        PostRender(chunk, localPos, globalPos);
    }

    public virtual void AddBlockData    (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
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
        BlockConfig config = world.blockIndex.GetConfig(type);
        Assert.IsTrue(config!=null);
        Block block = (Block)Activator.CreateInstance(config.blockClass);
        block.type = (ushort)type;
        block.config = config;
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