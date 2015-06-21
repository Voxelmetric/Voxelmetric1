using UnityEngine;
using System.Collections;

public class BlockSolid : BlockController
{

    public BlockSolid() : base() { }

    public override void AddBlockData(Chunk chunk, BlockPos pos, MeshData meshData, Block block)
    {
        if (!chunk.GetBlock(pos.Add(0, 1, 0)).controller.IsSolid(Direction.down))
            BuildFace(chunk, pos, meshData, Direction.up, block);

        if (!chunk.GetBlock(pos.Add(0, -1, 0)).controller.IsSolid(Direction.up))
            BuildFace(chunk, pos, meshData, Direction.down, block);

        if (!chunk.GetBlock(pos.Add(0, 0, 1)).controller.IsSolid(Direction.south))
            BuildFace(chunk, pos, meshData, Direction.north, block);

        if (!chunk.GetBlock(pos.Add(0, 0, -1)).controller.IsSolid(Direction.north))
            BuildFace(chunk, pos, meshData, Direction.south, block);

        if (!chunk.GetBlock(pos.Add(1, 0, 0)).controller.IsSolid(Direction.west))
            BuildFace(chunk, pos, meshData, Direction.east, block);

        if (!chunk.GetBlock(pos.Add(-1, 0, 0)).controller.IsSolid(Direction.east))
            BuildFace(chunk, pos, meshData, Direction.west, block);
    }

    public virtual void BuildFace(Chunk chunk, BlockPos pos, MeshData meshData, Direction direction, Block block)
    {
        BlockBuilder.BuildRenderer(chunk, pos, meshData, direction);
        BlockBuilder.BuildTexture(chunk, pos, meshData, direction, Config.Textures.Stone);
        BlockBuilder.BuildColors(chunk, pos, meshData, direction);
        if (Config.Toggle.UseCollisionMesh)
        {
            BlockBuilder.BuildCollider(chunk, pos, meshData, direction);
        }
    }

    public override string Name() { return "solid"; }

    public override bool IsSolid(Direction direction) { return true; }
}