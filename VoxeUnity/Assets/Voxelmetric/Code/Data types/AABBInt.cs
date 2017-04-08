namespace Voxelmetric.Code.Data_types
{
    public struct AABBInt
    {
        public readonly int minX;
        public readonly int minY;
        public readonly int minZ;
        public readonly int maxX;
        public readonly int maxY;
        public readonly int maxZ;

        public AABBInt(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            minX = x1;
            minY = y1;
            minZ = z1;
            maxX = x2;
            maxY = y2;
            maxZ = z2;
        }
    }
}
