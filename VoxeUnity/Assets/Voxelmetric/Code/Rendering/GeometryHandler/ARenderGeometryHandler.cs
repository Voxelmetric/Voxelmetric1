using UnityEngine;
using Voxelmetric.Code.Rendering.GeometryBatcher;

namespace Voxelmetric.Code.Rendering.GeometryHandler
{
    public abstract class ARenderGeometryHandler: IGeometryHandler<Material>
    {
        public RenderGeometryBatcher Batcher { get; private set; }

        public ARenderGeometryHandler()
        {
            Batcher = new RenderGeometryBatcher();
        }

        public void Reset()
        {
            Batcher.Clear();
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public abstract void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ);

        public abstract void Commit();
    }
}
