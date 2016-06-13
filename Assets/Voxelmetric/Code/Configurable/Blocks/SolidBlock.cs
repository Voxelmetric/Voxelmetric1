using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class SolidBlock : Block
{
    public virtual bool solidTowardsSameType { get { return ((SolidBlockConfig)config).solidTowardsSameType; } }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos)
    {
        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            Block adjacentBlock = blocks.GetBlock(globalPos.Add(dir));
            if (!adjacentBlock.IsSolid(DirectionUtils.Opposite(dir)))
            {
                if (solid || !solidTowardsSameType || adjacentBlock.type != type)
                {
                    BuildFace(chunk, localPos, globalPos, dir);
                }
            }
        }
    }

    protected virtual void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, Direction direction) { }

}
