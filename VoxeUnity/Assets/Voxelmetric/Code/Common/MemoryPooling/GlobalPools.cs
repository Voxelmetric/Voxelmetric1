using System.Collections.Generic;
using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public class GlobalPools: AObjectPool
    {
        public readonly ObjectPool<Chunk> ChunkPool =
            new ObjectPool<Chunk>(ch => new Chunk(), 1024, true);

        public readonly ObjectPool<Mesh> MeshPool =
            new ObjectPool<Mesh>(m => new Mesh(), 128, true);

        private readonly Dictionary<int, IArrayPool<Vector2>> m_vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        private readonly Dictionary<int, IArrayPool<Vector4>> m_vector4ArrayPools =
            new Dictionary<int, IArrayPool<Vector4>>(128);

        private readonly Dictionary<int, IArrayPool<Color32>> m_color32ArrayPools =
            new Dictionary<int, IArrayPool<Color32>>(128);

        private readonly Dictionary<int, IArrayPool<ushort>> m_ushortArrayPools =
            new Dictionary<int, IArrayPool<ushort>>(128);

        public Vector2[] PopVector2Array(int size)
        {
            return PopArray(size, m_vector2ArrayPools);
        }

        public Vector3[] PopVector3Array(int size)
        {
            return PopArray(size, m_vector3ArrayPools);
        }

        public Vector4[] PopVector4Array(int size)
        {
            return PopArray(size, m_vector4ArrayPools);
        }

        public Color32[] PopColor32Array(int size)
        {
            return PopArray(size, m_color32ArrayPools);
        }

        public ushort[] PopUshortArray(int size)
        {
            return PopArray(size, m_ushortArrayPools);
        }

        public void PushVector2Array(Vector2[] arr)
        {
            PushArray(arr, m_vector2ArrayPools);
        }

        public void PushVector3Array(Vector3[] arr)
        {
            PushArray(arr, m_vector3ArrayPools);
        }

        public void PushVector4Array(Vector4[] arr)
        {
            PushArray(arr, m_vector4ArrayPools);
        }

        public void PushColor32Array(Color32[] arr)
        {
            PushArray(arr, m_color32ArrayPools);
        }

        public void PushUshortArray(ushort[] arr)
        {
            PushArray(arr, m_ushortArrayPools);
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ChunkPool=");
            sb.Append(ChunkPool.Capacity);
            sb.Append(",MeshPool=");
            sb.Append(MeshPool.Capacity);
            sb.Append(",Vec2Arr=");
            sb.Append(m_vector2ArrayPools.Count);
            sb.Append(",Vec3Arr=");
            sb.Append(m_vector3ArrayPools.Count);
            sb.Append(",Vec4Arr=");
            sb.Append(m_vector4ArrayPools.Count);
            sb.Append(",ColorArr=");
            sb.Append(m_color32ArrayPools.Count);
            return sb.ToString();
        }
    }
}
