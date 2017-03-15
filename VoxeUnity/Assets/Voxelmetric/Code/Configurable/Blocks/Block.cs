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

    public string Name
    {
        get { return config.name; }
    }

    public virtual string DisplayName
    {
        get { return Name; }
    }

    public bool Solid
    {
        get { return config.solid; }
    }

    public bool Transparent
    {
        get { return config.transparent; }
    }

    public bool CanBeWalkedOn
    {
        get { return config.canBeWalkedOn; }
    }

    public bool CanBeWalkedThrough
    {
        get { return config.canBeWalkedThrough; }
    }

    public bool Custom
    {
        get { return config.custom; }
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
        return Name;
    }
}