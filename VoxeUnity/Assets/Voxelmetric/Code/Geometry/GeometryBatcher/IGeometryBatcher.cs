using UnityEngine;

namespace Voxelmetric.Code.Geometry.GeometryBatcher
{
    public interface IGeometryBatcher
    {
        void Clear();
        void Commit(Vector3 position, Quaternion rotation
#if DEBUG
            , string debugName = null
#endif
            );

        bool Enabled { get; set; }
    }
}
