namespace Voxelmetric.Code.Rendering.GeometryHandler
{
    public interface IGeometryHandler<in T> where T: class
    {
        void Reset();

        /// <summary> Updates the chunk based on its contents </summary>
        void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ);

        void Commit();
    }
}