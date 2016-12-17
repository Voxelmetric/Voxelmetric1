using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities.Noise;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools: AObjectPool
    {
        public NoiseItem[] noiseItems;

        private readonly ObjectPool<VertexData> m_vertexDataPool =
            new ObjectPool<VertexData>(ch => new VertexData(), 65535, false);

        private readonly Dictionary<int, IArrayPool<VertexData>> m_vertexDataArrayPools =
            new Dictionary<int, IArrayPool<VertexData>>(128);

        private readonly Dictionary<int, IArrayPool<VertexDataFixed>> m_vertexDataFixedArrayPools =
            new Dictionary<int, IArrayPool<VertexDataFixed>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        private readonly Dictionary<int, IArrayPool<bool>> m_boolArrayPools =
            new Dictionary<int, IArrayPool<bool>>(128);

        private readonly Dictionary<int, IArrayPool<float>> m_floatArrayPools =
            new Dictionary<int, IArrayPool<float>>(128);

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

        public Vector3[] PopVector3Array(int size)
        {
            return PopArray(size, m_vector3ArrayPools);
        }

        public bool[] PopBoolArray(int size)
        {
            return PopArray(size, m_boolArrayPools);
        }

        public float[] PopFloatArray(int size)
        {
            return PopArray(size, m_floatArrayPools);
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

        public void PushVector3Array(Vector3[] arr)
        {
            PushArray(arr, m_vector3ArrayPools);
        }

        public void PushBoolArray(bool[] arr)
        {
            PushArray(arr, m_boolArrayPools);
        }

        public void PushFloatArray(float[] arr)
        {
            PushArray(arr, m_floatArrayPools);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("VertexData=");
            sb.Append(m_vertexDataPool.Capacity);
            sb.Append(",VertexDataArray=");
            sb.Append(m_vertexDataArrayPools.Count);
            sb.Append(",VertexDataFixed=");
            sb.Append(m_vertexDataFixedArrayPools.Count);
            sb.Append(",Vec3Arr=");
            sb.Append(m_vector3ArrayPools.Count);
            sb.Append(",BoolArr=");
            sb.Append(m_boolArrayPools.Count);
            sb.Append(",FloatArr=");
            sb.Append(m_floatArrayPools.Count);
            sb.Append(",MarshaledBLeft=");
            sb.Append(m_marshaledPool.Left);
            return sb.ToString();
        }
    }
}
