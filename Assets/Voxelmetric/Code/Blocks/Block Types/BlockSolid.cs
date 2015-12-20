using UnityEngine;
using System.Collections;

public class BlockSolid : Block
{
    public virtual bool solidTowardsSameType { get { return ((SolidBlockConfig)config).solidTowardsSameType; } }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            if (!chunk.LocalGetBlock(localPos.Add(dir)).IsSolid(DirectionUtils.Opposite(dir)))
            {
                if ((solid || !solidTowardsSameType || chunk.LocalGetBlock(localPos.Add(dir)).type != type))
                {
                    BuildFace(chunk, localPos, globalPos, meshData, dir);
                    break;
                }
            }
        }

        BuildAlways(chunk, localPos, globalPos, meshData);
    }

    public virtual void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction) { }
    public virtual void BuildAlways(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData) { }

}
