namespace Assets.Voxelmetric.Code.Core.GeometryBatcher
{
    public interface IGeometryBatcher
    {
        void Clear();
        void Commit();
        void Enable(bool enable);
        bool IsEnabled();
    }
}
