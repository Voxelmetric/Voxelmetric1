namespace Voxelmetric.Code.Core
{
    public interface IChunkGeometryHandler
    {
        void Reset();

        /// <summary> Updates the chunk based on its contents </summary>
        void Build();

        void Commit();
    }
}