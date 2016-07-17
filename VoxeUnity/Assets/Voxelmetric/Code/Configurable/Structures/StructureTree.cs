using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class StructureTree: GeneratedStructure
{
    protected WorldBlocks blocks;
    protected BlockData leaves;
    protected BlockData log;

    public StructureTree()
    {
        negX = 3;
        posX = 3;
        negZ = 3;
        posZ = 3;
        posY = 6;
        negY = 0;
    }

    public override void Init(World world)
    {
        blocks = world.blocks;
        leaves = new BlockData(world.blockProvider.GetBlock("leaves").type);
        log = new BlockData(world.blockProvider.GetBlock("log").type);
    }

    public override void Build(World world, Chunk chunk, Vector3Int pos, TerrainLayer layer)
    {
        int noise = layer.GetNoise(pos.x, pos.y, pos.z, 1f, 3, 1);
        int leavesRange = noise + 3;
        int leavesRange2 = leavesRange*leavesRange;
        int trunkHeight = posY - noise;

        float a2inv = 1.0f / leavesRange2;
        float b2inv = 1.0f / ((leavesRange-1)*(leavesRange-1));

        for (int y = -leavesRange+1; y <= leavesRange-1; y++)
        {
            for (int z = -leavesRange; z <= leavesRange; z++)
            {
                for (int x = -leavesRange; x <= leavesRange; x++)
                {
                    if (x*x*a2inv +z*z*a2inv + y*y*b2inv<=1.0f) // An ellipsoid flattened on the y axis
                    {
                        blocks.Set(pos.Add(x, y+ trunkHeight, z), leaves);
                    }
                }
            }
        }

        blocks.Set(pos, log);
        for (int y = 1; y <= trunkHeight; y++)
        {
            blocks.Set(pos.Add(0, y, 0), log);
        }
    }
}