using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Configurable.Blocks
{
    public struct BlockFace // 26B (32B)
    {
        public Vector3Int pos; // 12B
        public Block block; // 8B
        public int materialID; // 4B        
        public Direction side; // 1B
        public BlockLightData light; //1B
    }
}
