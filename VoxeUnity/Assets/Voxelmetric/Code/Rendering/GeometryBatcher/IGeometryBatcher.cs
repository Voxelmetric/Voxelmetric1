using UnityEngine;

namespace Voxelmetric.Code.Rendering.GeometryBatcher
{
    public interface IGeometryBatcher<in T> where T: class
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
