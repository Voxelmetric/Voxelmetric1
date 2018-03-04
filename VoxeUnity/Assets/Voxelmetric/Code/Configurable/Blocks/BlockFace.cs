using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Configurable.Blocks
{
    public struct BlockFace // 24B
    {
        public Vector3Int pos; // 12B
        public int materialID; // 4B
        public Block block; // 2B
        public Direction side; // 1B
        public BlockLightData light; //1B
    }
}
