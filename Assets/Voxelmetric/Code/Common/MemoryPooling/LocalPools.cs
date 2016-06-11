using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools: AObjectPool
    {
        private readonly ObjectPool<VertexData> m_vertexDataPool =
            new ObjectPool<VertexData>(ch => new VertexData(), 65535, false);

        private readonly Dictionary<int, IArrayPool<VertexData>> m_vertexDataArrayPools =
            new Dictionary<int, IArrayPool<VertexData>>(128);

        private readonly Dictionary<int, IArrayPool<BlockData>> m_blockDataArrayPools =
            new Dictionary<int, IArrayPool<BlockData>>(128);

        private readonly Dictionary<int, IArrayPool<Vector2>> m_vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        public VertexData PopVertexData()
        {
            return m_vertexDataPool.Pop();
        }

        public VertexData[] PopVertexDataArray(int size)
        {
            return PopArray(size, m_vertexDataArrayPools);
        }

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

        public void PushVertexData(VertexData item)
        {
            m_vertexDataPool.Push(item);
        }

        public void PushVertexDataArray(VertexData[] arr)
        {
            PushArray(arr, m_vertexDataArrayPools);
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
