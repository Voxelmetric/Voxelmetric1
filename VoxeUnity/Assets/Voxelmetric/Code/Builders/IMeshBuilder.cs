using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Builders
{
    public interface IMeshBuilder
    {
        void Build(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ);
    }
}
