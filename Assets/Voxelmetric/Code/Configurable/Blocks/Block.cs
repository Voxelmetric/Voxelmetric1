using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

public class Block
{
    public ushort type;
    public BlockConfig config;

    public Block()
    {
        type = 0;
        config = null;
    }

    public Block(int type, BlockConfig config)
    {
        Assert.IsTrue(config!=null);
        this.type = (ushort)type;
        this.config = config;
    }

    public virtual string name
    {
        get { return config.name; }
    }

    public virtual string displayName
    {
        get { return name; }
    }

    public virtual bool solid
    {
        get { return config.solid; }
    }

    public virtual bool canBeWalkedOn
    {
        get { return config.canBeWalkedOn; }
    }

    public virtual bool canBeWalkedThrough
    {
        get { return config.canBeWalkedThrough; }
    }

    public virtual bool IsSolid(Direction direction)
    {
        return solid;
    }
    
    public virtual void OnInit(BlockProvider blockProvider)
    {
    }

    public virtual void BuildBlock(Chunk chunk, BlockPos localpos, BlockPos globalPos)
    {
    }

    public virtual bool CanMergeFaceWith(Block adjacentBlock, Direction dir)
    {
        return true;
    }

    public virtual void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, Direction dir)
    {
    }

    public virtual void OnCreate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
    }

    public virtual void OnDestroy(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
    }

    public virtual void RandomUpdate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
    }

    public virtual void ScheduledUpdate(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
    }

    public virtual bool RaycastHit(Vector3 pos, Vector3 dir, BlockPos bPos)
    {
        return solid;
    }

    public override string ToString()
    {
        return name;
    }
}