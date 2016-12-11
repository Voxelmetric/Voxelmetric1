using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Memory;
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

        private readonly Dictionary<int, IArrayPool<VertexDataFixed>> m_vertexDataFixedArrayPools =
            new Dictionary<int, IArrayPool<VertexDataFixed>>(128);

        private readonly Dictionary<int, IArrayPool<Block>> m_blockArrayPools =
            new Dictionary<int, IArrayPool<Block>>(128);

        private readonly Dictionary<int, IArrayPool<Vector2>> m_vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        private readonly Dictionary<int, IArrayPool<byte>> m_byteArrayPools =
            new Dictionary<int, IArrayPool<byte>>(128);

        private readonly Dictionary<int, IArrayPool<bool>> m_boolArrayPools =
            new Dictionary<int, IArrayPool<bool>>(128);

        private readonly MarshalMemPool m_marshaledPool = new MarshalMemPool(1024*512); // 512 kiB of memory should be more sufficient for now

        public MarshalMemPool MarshaledPool
        {
            get { return m_marshaledPool; }
        }

        public VertexData PopVertexData()
        {
            return m_vertexDataPool.Pop();
        }

        public VertexData[] PopVertexDataArray(int size)
        {
            return PopArray(size, m_vertexDataArrayPools);
        }

        public VertexDataFixed[] PopVertexDataFixedArray(int size)
        {
            return PopArray(size, m_vertexDataFixedArrayPools);
        }

        public Block[] PopBlockArray(int size)
        {
            return PopArray(size, m_blockArrayPools);
        }

        public Vector2[] PopVector2Array(int size)
        {
            return PopArray(size, m_vector2ArrayPools);
        }

        public Vector3[] PopVector3Array(int size)
        {
            return PopArray(size, m_vector3ArrayPools);
        }

        public byte[] PopByteArray(int size)
        {
            return PopArray(size, m_byteArrayPools);
        }

        public bool[] PopBoolArray(int size)
        {
            return PopArray(size, m_boolArrayPools);
        }

        public void PushVertexData(VertexData item)
        {
            m_vertexDataPool.Push(item);
        }

        public void PushVertexDataArray(VertexData[] arr)
        {
            PushArray(arr, m_vertexDataArrayPools);
        }

        public void PushVertexDataFixedArray(VertexDataFixed[] arr)
        {
            PushArray(arr, m_vertexDataFixedArrayPools);
        }

        public void PushBlockArray(Block[] arr)
        {
            PushArray(arr, m_blockArrayPools);
        }

        public void PushVector2Array(Vector2[] arr)
        {
            PushArray(arr, m_vector2ArrayPools);
        }

        public void PushVector3Array(Vector3[] arr)
        {
            PushArray(arr, m_vector3ArrayPools);
        }

        public void PushByteArray(byte[] arr)
        {
            PushArray(arr, m_byteArrayPools);
        }

        public void PushBoolArray(bool[] arr)
        {
            PushArray(arr, m_boolArrayPools);
        }
    }
}
