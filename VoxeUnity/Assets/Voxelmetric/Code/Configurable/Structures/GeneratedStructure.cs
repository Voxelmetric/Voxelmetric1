using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class GeneratedStructure
{
    public int posX = 0;
    public int negX = 0;

    public int posY = 0;
    public int negY = 0;

    public int posZ = 0;
    public int negZ = 0;
    public virtual void Init(World world) {}

    public virtual void Build(World world, Chunk chunk, Vector3Int pos, TerrainLayer layer) { }
}