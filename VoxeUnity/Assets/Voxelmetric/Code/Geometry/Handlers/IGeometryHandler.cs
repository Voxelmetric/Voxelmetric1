namespace Voxelmetric.Code.Geometry.GeometryHandler
{
    public interface IGeometryHandler
    {
        void Reset();

        /// <summary> Updates the chunk based on its contents </summary>
        void Build();

        void Commit();
    }
}