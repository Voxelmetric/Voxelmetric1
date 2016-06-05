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

    public override void Build(World world, Chunk chunk, BlockPos pos, TerrainLayer layer)
    {
        int leaves = layer.GetNoise(pos.x, 0, pos.z, 1f, 2, 1) +1;

        for (int x = -leaves; x <= leaves; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -leaves; z <= leaves; z++)
                {
                    if (pos.Add(x, y, z) < chunk.pos + new BlockPos(Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize) && pos.Add(x, y, z) >= chunk.pos)
                    {
                        Block block = Block.Create("leaves", world);
                        world.blocks.Set(pos.Add(x, y, z), block, updateChunk: false, setBlockModified: false);
                    }
                }
            }
        }
        for (int y = 0; y <= 5; y++)
        {
            if (pos.Add(0, y, 0) < chunk.pos + new BlockPos(Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize)
                && pos.Add(0, y, 0) >= chunk.pos)
            {
                Block block = Block.Create("log", world);
                world.blocks.Set(pos.Add(0, y, 0), block, updateChunk: false, setBlockModified: false);
            }
        }
    }

}
