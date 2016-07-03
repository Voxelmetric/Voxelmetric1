namespace Voxelmetric.Code.Core
{
    public interface IChunkGeometryHandler
    {
        void Reset();

        /// <summary> Updates the chunk based on its contents </summary>
        void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ);

        void Commit();
    }
}