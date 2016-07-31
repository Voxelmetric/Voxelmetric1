using UnityEngine;
using Voxelmetric.Code.Rendering.GeometryBatcher;

namespace Voxelmetric.Code.Rendering.GeometryHandler
{
    public abstract class AColliderGeometryHandler: IGeometryHandler<PhysicMaterial>
    {
        public ColliderGeometryBatcher Batcher { get; private set; }

        public AColliderGeometryHandler(string prefabName)
        {
            Batcher = new ColliderGeometryBatcher(prefabName);
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
