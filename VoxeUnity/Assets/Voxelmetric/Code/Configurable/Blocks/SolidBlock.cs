using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class SolidBlock : Block
{
    public override void BuildBlock(Chunk chunk, ref Vector3Int localPos, int materialID)
    {
        ChunkBlocks blocks = chunk.blocks;

        for (int d = 0; d < 6; d++)
        {
            Direction dir = DirectionUtils.Get(d);
            Vector3Int blockPos = localPos.Add(dir);
            Block adjacentBlock = blocks.GetBlock(ref blockPos);

            BlockFace face = new BlockFace
            {
                block = blocks.GetBlock(ref blockPos),
                pos = blockPos,
                side = dir,
                light = new BlockLightData(0),
            };

            if (CanBuildFaceWith(adjacentBlock))
                BuildFace(chunk, null, ref face);
        }
    }
}
