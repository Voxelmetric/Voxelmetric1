using UnityEngine;
using System.Collections;

public class Block
{
    public static int health = 100;
    public static int toughness = 100;
    public static bool canBeWalkedOn = false;

    //Base block constructor
    public Block() { OnCreate(); }

    public virtual void AddBlockData (Chunk chunk, BlockPos pos, MeshData meshData) { }

    public virtual void BuildBlock(Chunk chunk, BlockPos pos, MeshData meshData)
    {
        PreRender(chunk, pos);
        AddBlockData(chunk, pos, meshData);
        PostRender(chunk, pos);
    }

    public virtual void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction) { }

    public virtual bool IsSolid(Direction direction) { return false; }

    public virtual void OnCreate() { }

    public virtual void PreRender(Chunk chunk, BlockPos pos) { }

    public virtual void PostRender(Chunk chunk, BlockPos pos) { }

    public virtual void OnDestroy() { }

}