using JetBrains.Annotations;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

public class Block
{
    protected BlockConfig config;

    public ushort type;
    public string Name;
    public bool Solid;
    public bool CanBeWalkedOn;
    public bool CanBeWalkedThrough;
    public bool Custom;

    public Block()
    {
        type = 0;
        config = null;
    }

    public void Init(ushort type, BlockConfig config)
    {
        Assert.IsTrue(config!=null);
        this.type = type;
        this.config = config;

        Name = config.name;
        Solid = config.solid;
        CanBeWalkedOn = config.canBeWalkedOn;
        CanBeWalkedThrough = config.canBeWalkedThrough;
        Custom = config.custom;
    }
    
    public virtual string DisplayName
    {
        get { return Name; }
    }

    public virtual void OnInit(BlockProvider blockProvider)
    {
    }

    public virtual void BuildBlock(Chunk chunk, Vector3Int localpos)
    {
    }

    public virtual bool CanBuildFaceWith(Block adjacentBlock)
    {
        return config.custom; // custom blocks will be considered as able to create face with others by default
    }

    public virtual bool CanMergeFaceWith(Block adjacentBlock)
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