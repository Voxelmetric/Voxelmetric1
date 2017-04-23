using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Configurable.Blocks
{
    public struct BlockFace
    {
        public Block block;
        public Vector3Int pos;
        public Direction side;
        public BlockLightData light;
        public int materialID;
    }
}
