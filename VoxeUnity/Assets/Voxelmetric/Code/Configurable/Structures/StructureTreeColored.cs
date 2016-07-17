using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

public class StructureTreeColored: StructureTree
{
    public override void Init(World world)
    {
        //Block block = BlockColored.SetBlockColor(new Block("coloredblock", world), 57, 100, 49);
        //world.SetBlock(pos.Add(x, y, z), block, updateChunk: false, setBlockModified: false);
        leaves = new BlockData(BlockProvider.AirType);
        //Block block = BlockColored.SetBlockColor(new Block("coloredblock", world), 66, 44, 17);
        //world.SetBlock(pos.Add(0, y, 0), block, updateChunk: false, setBlockModified: false);
        log = new BlockData(BlockProvider.AirType);
    }

    public override void Build(World world, Chunk chunk, Vector3Int pos, TerrainLayer layer)
    {
    }
}