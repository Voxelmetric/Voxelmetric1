using UnityEngine;
using System.Collections;

public class BlockSolid : BlockController
{
    public bool isSolid = true;
    public bool solidTowardsSameType = true;

    public BlockSolid() : base() { }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Block block)
    {
        if (!chunk.LocalGetBlock(localPos.Add(0, 1, 0)).IsSolid(Direction.down) && (isSolid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(0, 1, 0)) != block))
            BuildFace(chunk, localPos, globalPos, meshData, Direction.up, block);

        if (!chunk.LocalGetBlock(localPos.Add(0, -1, 0)).IsSolid(Direction.up) && (isSolid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(0, -1, 0)) != block))
            BuildFace(chunk, localPos, globalPos, meshData, Direction.down, block);

        if (!chunk.LocalGetBlock(localPos.Add(0, 0, 1)).IsSolid(Direction.south) && (isSolid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(0, 0, 1)) != block))
            BuildFace(chunk, localPos, globalPos, meshData, Direction.north, block);

        if (!chunk.LocalGetBlock(localPos.Add(0, 0, -1)).IsSolid(Direction.north) && (isSolid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(0, 0, -1)) != block))
            BuildFace(chunk, localPos, globalPos, meshData, Direction.south, block);

        if (!chunk.LocalGetBlock(localPos.Add(1, 0, 0)).IsSolid(Direction.west) && (isSolid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(1, 0, 0)) != block))
            BuildFace(chunk, localPos, globalPos, meshData, Direction.east, block);

        if (!chunk.LocalGetBlock(localPos.Add(-1, 0, 0)).IsSolid(Direction.east) && (isSolid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(-1, 0, 0)) != block))
            BuildFace(chunk, localPos, globalPos, meshData, Direction.west, block);
    }

    public virtual void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction, Block block)
    {
       
    }

    public override string Name(Block block) { return "solid"; }

    public override bool IsSolid(Block block, Direction direction) { return true; }

    public override bool CanBeWalkedOn(Block block) { return true; }

    public override bool CanBeWalkedThrough(Block block) { return false; }
}