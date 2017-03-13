namespace Voxelmetric.Code.Rendering.GeometryHandler
{
    public interface IGeometryHandler
    {
        void Reset();

        /// <summary> Updates the chunk based on its contents </summary>
        void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ);

        void Commit();
    }
}