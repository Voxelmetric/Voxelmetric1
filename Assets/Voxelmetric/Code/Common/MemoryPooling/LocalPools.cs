using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools: AObjectPool
    {
        private readonly Dictionary<int, IArrayPool<BlockData>> m_blockDataArrayPools =
            new Dictionary<int, IArrayPool<BlockData>>(128);

        private readonly Dictionary<int, IArrayPool<Vector2>> m_vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);
        
        public BlockData[] PopBlockDataArray(int size)
        {
            return PopArray(size, m_blockDataArrayPools);
        }

        public Vector2[] PopVector2Array(int size)
        {
            return PopArray(size, m_vector2ArrayPools);
        }

        public Vector3[] PopVector3Array(int size)
        {
            return PopArray(size, m_vector3ArrayPools);
        }
        
        public void PushBlockDataArray(BlockData[] arr)
        {
            PushArray(arr, m_blockDataArrayPools);
        }

        public void PushVector2Array(Vector2[] arr)
        {
            PushArray(arr, m_vector2ArrayPools);
        }

        public void PushVector3Array(Vector3[] arr)
        {
            PushArray(arr, m_vector3ArrayPools);
        }
    }
}
