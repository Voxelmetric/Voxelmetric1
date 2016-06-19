using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

public class StructureTreeColored : GeneratedStructure
{
    public StructureTreeColored()
    {
        negX = 3;
        posX = 3;
        negZ = 3;
        posZ = 3;
        posY = 5;
        negY = 0;
    }

    public override void Build(World world, Chunk chunk, BlockPos pos, TerrainLayer layer)
    {
        int leaves = layer.GetNoise(pos.x, 0, pos.z, 1f, 2, 1) + 1;

        for (int x = -leaves; x <= leaves; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -leaves; z <= leaves; z++)
                {
                    if (pos.Add(x, y, z) < chunk.pos + new BlockPos(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize)
                        && pos.Add(x, y, z) >= chunk.pos)
                    {
                        //Block block = BlockColored.SetBlockColor(new Block("coloredblock", world), 57, 100, 49);
                        //world.SetBlock(pos.Add(x, y, z), block, updateChunk: false, setBlockModified: false);
                    }
                }
            }
        }
        for (int y = 0; y <= 5; y++)
        {
            if (y < world.config.maxY)
            {
                if (pos.Add(0, y, 0) < chunk.pos + new BlockPos(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize)
                    && pos.Add(0, y, 0) >= chunk.pos)
                {
                    //Block block = BlockColored.SetBlockColor(new Block("coloredblock", world), 66, 44, 17);
                    //world.SetBlock(pos.Add(0, y, 0), block, updateChunk: false, setBlockModified: false);
                }
            }
        }
    }
}