using UnityEngine;
using SimplexNoise;

public class StructureTree: GeneratedStructure {

    public StructureTree()
    {
        negX = 3;
        posX = 3;
        negZ = 3;
        posZ = 3;
        posY = 5;
        negY = 0;
    }

    public override void Build(World world, BlockPos chunkPos, BlockPos pos, TerrainLayer layer)
    {
        int leaves = layer.GetNoise(pos.x, 0, pos.z, 1f, 2, 1) +1;

        for (int x = -leaves; x <= leaves; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -leaves; z <= leaves; z++)
                {
                    if (Chunk.InRange(pos.x + x - chunkPos.x) && Chunk.InRange(pos.z + z - chunkPos.z))
                    {
                        Block block = "leaves";
                        block.modified = false;
                        world.SetBlock(pos.Add(x, y, z), block, false);
                    }
                }
            }
        }
        for (int y = 0; y <= 5; y++)
        {
            if (y < Config.Env.WorldMaxY)
            {
                Block block = "log";
                block.modified = false;
                world.SetBlock(pos.Add(0, y, 0), block, false);
            }
        }
    }

    public void OldBuild(World world, BlockPos chunkPos, BlockPos pos, OldTerrainGen gen)
    {
        int leaves = gen.GetNoise(pos.x, 0, pos.z, 1f, 2, 1) + 1;
        pos = pos.Add(chunkPos);

        for (int x = -leaves; x <= leaves; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -leaves; z <= leaves; z++)
                {
                    if (Chunk.InRange(pos.x + x - chunkPos.x) && Chunk.InRange(pos.z + z - chunkPos.z))
                    {
                        Block block = "leaves";
                        block.modified = false;
                        world.SetBlock(pos.Add(x, y, z), block, false);
                    }
                }
            }
        }
        for (int y = 0; y <= 5; y++)
        {
            if (y < Config.Env.WorldMaxY)
            {
                Block block = "log";
                block.modified = false;
                world.SetBlock(pos.Add(0, y, 0), block, false);
            }
        }
    }

}
