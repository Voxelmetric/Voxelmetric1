using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

public class StructureTree: GeneratedStructure
{
    private BlockData leaves;
    private BlockData log;

    public StructureTree()
    {
        negX = 3;
        posX = 3;
        negZ = 3;
        posZ = 3;
        posY = 5;
        negY = 0;
    }

    public override void Init(World world)
    {
        leaves = new BlockData(world.blockProvider.GetBlock("leaves").type);
        log = new BlockData(world.blockProvider.GetBlock("log").type);
    }

    public override void Build(World world, Chunk chunk, Vector3Int pos, TerrainLayer layer)
    {
        int leavesRange = layer.GetNoise(pos.x, 0, pos.z, 1f, 2, 1) +1;
        for (int x = -leavesRange; x <= leavesRange; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -leavesRange; z <= leavesRange; z++)
                {
                    if (pos.Add(x, y, z) < chunk.pos + new Vector3Int(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize) && pos.Add(x, y, z) >= chunk.pos)
                    {
                        world.blocks.Set(pos.Add(x, y, z), leaves);
                    }
                }
            }
        }
        for (int y = 0; y <= 5; y++)
        {
            if (pos.Add(0, y, 0) < chunk.pos + new Vector3Int(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize)
                && pos.Add(0, y, 0) >= chunk.pos)
            {
                world.blocks.Set(pos.Add(0, y, 0), log);
            }
        }
    }
}