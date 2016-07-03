using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Core
{
    public class ColliderGeometryHandler: IChunkGeometryHandler
    {
        private Chunk chunk;

        public ColliderGeometryBatcher Batcher { get; private set; }

        public ColliderGeometryHandler(Chunk chunk)
        {
            this.chunk = chunk;
            Batcher = new ColliderGeometryBatcher(this.chunk);
        }

        public void Reset()
        {
            Batcher.Clear();
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            Globals.CubeMeshColliderBuilder.Build(chunk, minX, maxX, minY, maxY, minZ, maxZ);
        }

        public void Commit()
        {
            Batcher.Commit();
        }
    }
}