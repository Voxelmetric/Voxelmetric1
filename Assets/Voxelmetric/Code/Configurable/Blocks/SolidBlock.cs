using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class SolidBlock : Block
{
    public virtual bool solidTowardsSameType { get { return ((SolidBlockConfig)config).solidTowardsSameType; } }

    public override bool CanMergeFaceWith(Block adjacentBlock, Direction dir)
    {
        if (!adjacentBlock.IsSolid(DirectionUtils.Opposite(dir)))
        {
            if (solid || !solidTowardsSameType || adjacentBlock.type != type)
            {
                return true;
            }
        }

        return false;
    }

    public override void BuildBlock(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        WorldBlocks blocks = chunk.world.blocks;

        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            Block adjacentBlock = blocks.GetBlock(globalPos.Add(dir));
            if (CanMergeFaceWith(adjacentBlock, dir))
                BuildFace(chunk, localPos, globalPos, dir);
        }
    }
}
