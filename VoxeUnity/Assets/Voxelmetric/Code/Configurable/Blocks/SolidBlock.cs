using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class SolidBlock : Block
{
    public override void BuildBlock(Chunk chunk, Vector3Int localPos, int materialID)
    {
        ChunkBlocks blocks = chunk.blocks;

        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            Block adjacentBlock = blocks.GetBlock(localPos.Add(dir));
            if (CanBuildFaceWith(adjacentBlock))
                BuildFace(chunk, localPos, null, dir, materialID);
        }
    }
}
