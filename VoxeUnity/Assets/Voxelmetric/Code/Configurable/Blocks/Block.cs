using JetBrains.Annotations;
using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

public class Block
{
    protected BlockConfig Config;
    public string Name;
    public ushort Type;
    public int RenderMaterialID;
    public int PhysicMaterialID;
    public bool Solid;
    public bool CanBeWalkedOn;
    public bool CanBeWalkedThrough;
    public bool Custom;

    public Block()
    {
        Type = 0;
        Config = null;
    }

    public void Init(ushort type, [NotNull] BlockConfig config)
    {
        Type = type;
        Config = config;

        Name = config.name;
        Solid = config.solid;
        CanBeWalkedOn = config.canBeWalkedOn;
        CanBeWalkedThrough = config.canBeWalkedThrough;
        Custom = config.custom;

        RenderMaterialID = config.renderMaterialID;
        PhysicMaterialID = config.physicMaterialID;
    }
    
    public virtual string DisplayName
    {
        get { return Name; }
    }

    public virtual void OnInit(BlockProvider blockProvider)
    {
    }

    public virtual void BuildBlock(Chunk chunk, Vector3Int localpos, int materialID)
    {
    }

    public bool CanBuildFaceWith(Block adjacentBlock)
    {
        return adjacentBlock.Solid ? !Solid : (Solid || Type!=adjacentBlock.Type);
    }

    public bool CanMergeFaceWith(Block adjacentBlock)
    {
        return Type==adjacentBlock.Type;
    }

    public virtual void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face)
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
        return removalRequested ? Config.raycastHitOnRemoval : Config.raycastHit;
    }

    public override string ToString()
    {
        return Name;
    }
}