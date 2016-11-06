using UnityEngine;
using System.Collections;
using System;
using System.Runtime.Serialization;

[Serializable]
public class SolidBlock : Block
{
    public virtual bool solidTowardsSameType { get { return ((SolidBlockConfig)config).solidTowardsSameType; } }

    public SolidBlock() { }

    public override void AddBlockData(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData)
    {
        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            Block adjacentBlock = chunk.blocks.LocalGet(localPos.Add(dir));
            bool adjTrans = adjacentBlock.IsTransparent(DirectionUtils.Opposite(dir));
            bool adjSolid = adjacentBlock.IsSolid(DirectionUtils.Opposite(dir));
            if (!adjSolid || adjTrans)
            {
                if(solid && transparent && adjTrans && adjSolid)//usesConnectedTextures
                    ;
                else if (solid || !solidTowardsSameType || adjacentBlock.Type != Type)
                {
                    BuildFace(chunk, localPos, globalPos, meshData, dir);
                }
            }
        }

        BuildAlways(chunk, localPos, globalPos, meshData);
    }

    public virtual void BuildFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData, Direction direction) { }
    public virtual void BuildAlways(Chunk chunk, BlockPos localPos, BlockPos globalPos, MeshData meshData) { }

    // Constructor only used for deserialization
    protected SolidBlock(SerializationInfo info, StreamingContext context):
        base(info, context) {
    }
}
