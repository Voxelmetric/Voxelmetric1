using System.Collections.Generic;
using Assets.Voxelmetric.Code.Common.Memory;
using UnityEngine;

namespace Assets.Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public class GlobalPools: AObjectPool
    {
        public readonly ObjectPool<Chunk> ChunkPool =
            new ObjectPool<Chunk>(ch => Chunk.Create(null, BlockPos.zero), 128, false);

        public readonly ObjectPool<EmptyChunk> EmptyChunkPool =
            new ObjectPool<EmptyChunk>(ch => EmptyChunk.Create(null, BlockPos.zero), 128, false);

        public readonly ObjectPool<Mesh> MeshPool =
            new ObjectPool<Mesh>(m => new Mesh(), 128, false);
        
        private readonly Dictionary<int, IArrayPool<Vector2>> m_vector2ArrayPools =
            new Dictionary<int, IArrayPool<Vector2>>(128);

        private readonly Dictionary<int, IArrayPool<Vector3>> m_vector3ArrayPools =
            new Dictionary<int, IArrayPool<Vector3>>(128);

        private readonly Dictionary<int, IArrayPool<Vector4>> m_vector4ArrayPools =
            new Dictionary<int, IArrayPool<Vector4>>(128);

        private readonly Dictionary<int, IArrayPool<Color32>> m_color32ArrayPools =
            new Dictionary<int, IArrayPool<Color32>>(128);

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

        
    }
}
