using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class SolidBlock : Block
{
    public virtual bool solidTowardsSameType { get { return ((SolidBlockConfig)config).solidTowardsSameType; } }

    public override bool CanBuildFaceWith(Block adjacentBlock, Direction dir)
    {
        bool adjSolid = adjacentBlock.IsSolid(DirectionUtils.Opposite(dir));
        bool adjTransparent = adjacentBlock.IsTransparent(DirectionUtils.Opposite(dir));
        if ((!adjSolid || adjTransparent) && (!solid || !transparent || !adjTransparent || !adjSolid))
        {
            if (solid || !solidTowardsSameType || adjacentBlock.type!=type)
                return true;
        }

        return false;
    }

    public override void BuildBlock(Chunk chunk, Vector3Int localPos)
    {
        ChunkBlocks blocks = chunk.blocks;

        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            Block adjacentBlock = blocks.GetBlock(localPos.Add(dir));
            if (CanBuildFaceWith(adjacentBlock, dir))
                BuildFace(chunk, localPos, dir);
        }
    }
}
