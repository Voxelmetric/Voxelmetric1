namespace Voxelmetric.Code.Core
{
    public class ChunkBounds
    {
        public int minX;
        public int maxX;
        public int minY;
        public int maxY;
        public int minZ;
        public int maxZ;
        public int lowestEmptyBlock;

        public ChunkBounds()
        {
            Reset();
        }

        public void Reset()
        {
            minX = minY = minZ = 0;
            maxX = maxY = maxZ = Env.ChunkSize1;
            lowestEmptyBlock = 0;
        }

        public void Init()
        {
            minX = minY = minZ = Env.ChunkSize1;
            maxX = maxY = maxZ = 0;
            lowestEmptyBlock = Env.ChunkSize1;
        }
    }
}
