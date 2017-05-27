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
    public bool Custom;

    public bool CanCollide
    {
        get { return PhysicMaterialID >= 0; }
    }

    public Block()
    {
        Type = 0;
        Config = null;
    }

    public void Init(ushort type, BlockConfig config)
    {
        Type = type;
        Config = config;

        RenderMaterialID = config.renderMaterialID;
        PhysicMaterialID = config.physicMaterialID;

        Name = config.name;
        Solid = config.solid;
        Custom = false;
    }
    
    public virtual string DisplayName
    {
        get { return Name; }
    }

    public virtual void OnInit(BlockProvider blockProvider)
    {
    }

    public virtual void BuildBlock(Chunk chunk, ref Vector3Int localpos, int materialID)
    {
    }

    public bool CanBuildFaceWith(Block adjacentBlock)
    {
        return adjacentBlock.Solid ? !Solid : (Solid || Type!=adjacentBlock.Type);
    }

    public virtual void BuildFace(Chunk chunk, Vector3[] vertices, ref BlockFace face, bool rotated)
    {
    }

    public virtual void OnCreate(Chunk chunk, ref Vector3Int localPos)
    {
    }

    public virtual void OnDestroy(Chunk chunk, ref Vector3Int localPos)
    {
    }

    public virtual void RandomUpdate(Chunk chunk, ref Vector3Int localPos)
    {
    }

    public virtual void ScheduledUpdate(Chunk chunk, ref Vector3Int localPos)
    {
    }

    public bool RaycastHit(ref Vector3 pos, ref Vector3 dir, ref Vector3Int bPos, bool removalRequested)
    {
        return removalRequested ? Config.raycastHitOnRemoval : Config.raycastHit;
    }

    public override string ToString()
    {
        return Name;
    }
}