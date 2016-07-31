using UnityEngine;

namespace Voxelmetric.Code.Rendering.GeometryBatcher
{
    public interface IGeometryBatcher<in T> where T: class
    {
        void Clear();
        void Commit(Vector3 position, Quaternion rotation, T material
#if DEBUG
            , string debugName = null
#endif
            );
        void Enable(bool enable);
        bool IsEnabled();
    }
}
