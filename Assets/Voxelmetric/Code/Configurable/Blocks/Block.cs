using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

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
        Assert.IsTrue(config != null);
        this.type = (ushort)type;
        this.config = config;
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

    public virtual void OnInit          () { }
    public virtual void AddBlockData    (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void OnCreate        (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void PreRender       (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void PostRender      (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void OnDestroy       (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void RandomUpdate    (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual void ScheduledUpdate (Chunk chunk, BlockPos localPos, BlockPos globalPos) { }
    public virtual bool RaycastHit(Vector3 pos, Vector3 dir, BlockPos bPos) { return solid; } 
    
    public override string ToString()
    {
        return name;
    }
}