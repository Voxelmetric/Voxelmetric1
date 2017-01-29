using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

public class Block
{
    public ushort type;
    protected BlockConfig config;

    public Block()
    {
        type = 0;
        config = null;
    }

    public void Init(int type, BlockConfig config)
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

    public virtual bool transparent
    {
        get { return config.transparent; }
    }

    public virtual bool canBeWalkedOn
    {
        get { return config.canBeWalkedOn; }
    }

    public virtual bool canBeWalkedThrough
    {
        get { return config.canBeWalkedThrough; }
    }

    public virtual bool custom
    {
        get { return config.custom; }
    }

    public virtual bool IsSolid(Direction direction)
    {
        return solid;
    }

    public virtual bool IsTransparent(Direction direction)
    {
        return transparent;
    }

    public virtual void OnInit(BlockProvider blockProvider)
    {
    }

    public virtual void BuildBlock(Chunk chunk, Vector3Int localpos)
    {
    }

    public virtual bool CanBuildFaceWith(Block adjacentBlock, Direction dir)
    {
        return config.custom; // custom blocks will be considered as able to create face with others by default
    }

    public virtual bool CanMergeFaceWith(Block adjacentBlock, Direction dir)
    {
        return false;
    }

    public virtual void BuildFace(Chunk chunk, Vector3Int localPos, Vector3[] vertices, Direction dir)
    {
    }

    public virtual void OnCreate(Chunk chunk, Vector3Int localPos)
    {
    }

    public virtual void OnDestroy(Chunk chunk, Vector3Int localPos)
    {
    }

    public virtual void RandomUpdate(Chunk chunk, Vector3Int localPos)
    {
    }

    public virtual void ScheduledUpdate(Chunk chunk, Vector3Int localPos)
    {
    }

    public virtual bool RaycastHit(Vector3 pos, Vector3 dir, Vector3Int bPos, bool removalRequested)
    {
        return removalRequested ? config.raycastHitOnRemoval : config.raycastHit;
    }

    public override string ToString()
    {
        return name;
    }
}