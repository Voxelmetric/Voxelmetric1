using UnityEngine;
using System.Collections;

public class BlockSolid : BlockController
{
    public bool isSolid = true;
    public bool solidTowardsSameType = true;

    public BlockSolid() : base() { }

    public override void AddBlockData(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {
        if (!chunk.GetBlock(pos.Add(0, 1, 0)).controller.IsSolid(Direction.down) && (isSolid || !solidTowardsSameType || chunk.GetBlock(pos.Add(0, 1, 0)) != block))
            BuildFace(chunk, pos, meshData, Direction.up, block);

        if (!chunk.GetBlock(pos.Add(0, -1, 0)).controller.IsSolid(Direction.up) && (isSolid || !solidTowardsSameType || chunk.GetBlock(pos.Add(0, -1, 0)) != block))
            BuildFace(chunk, pos, meshData, Direction.down, block);

        if (!chunk.GetBlock(pos.Add(0, 0, 1)).controller.IsSolid(Direction.south) && (isSolid || !solidTowardsSameType || chunk.GetBlock(pos.Add(0, 0, 1)) != block))
            BuildFace(chunk, pos, meshData, Direction.north, block);

        if (!chunk.GetBlock(pos.Add(0, 0, -1)).controller.IsSolid(Direction.north) && (isSolid || !solidTowardsSameType || chunk.GetBlock(pos.Add(0, 0, -1)) != block))
            BuildFace(chunk, pos, meshData, Direction.south, block);

        if (!chunk.GetBlock(pos.Add(1, 0, 0)).controller.IsSolid(Direction.west) && (isSolid || !solidTowardsSameType || chunk.GetBlock(pos.Add(1, 0, 0)) != block))
            BuildFace(chunk, pos, meshData, Direction.east, block);

        if (!chunk.GetBlock(pos.Add(-1, 0, 0)).controller.IsSolid(Direction.east) && (isSolid || !solidTowardsSameType || chunk.GetBlock(pos.Add(-1, 0, 0)) != block))
            BuildFace(chunk, pos, meshData, Direction.west, block);
    }

    public virtual void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
       
    }

    public override string Name() { return "solid"; }

    public override bool IsSolid(Direction direction) { return true; }

    public override bool CanBeWalkedOn(Block block) { return true; }

    public override bool CanBeWalkedThrough(Block block) { return false; }
}