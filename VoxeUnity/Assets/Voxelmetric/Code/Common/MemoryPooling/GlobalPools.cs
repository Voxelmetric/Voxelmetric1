using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.StateManager;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Global object pools for often used heap objects.
    /// </summary>
    public class GlobalPools
    {
        public readonly ObjectPool<Chunk> ChunkPool =
            new ObjectPool<Chunk>(ch => new Chunk(), 1024, true);

        public readonly ObjectPool<Mesh> MeshPool =
            new ObjectPool<Mesh>(m => new Mesh(), 128, true);

        #region "Work items"
        /*
         * These need to be used with caution. Items are popped on the main thread and pushed back
         * on a separate thread.
         */
        public readonly ObjectPool<TaskPoolItem<ChunkStateManagerClient>> SMTaskPI =
            new ObjectPool<TaskPoolItem<ChunkStateManagerClient>>(m => new TaskPoolItem<ChunkStateManagerClient>(), 2048, false);
        
        public readonly ObjectPool<ThreadPoolItem<ChunkStateManagerClient>> SMThreadPI =
            new ObjectPool<ThreadPoolItem<ChunkStateManagerClient>>(m => new ThreadPoolItem<ChunkStateManagerClient>(), 2048, false);

        #endregion

        public readonly ArrayPoolCollection<Vector2> Vector2ArrayPool =
            new ArrayPoolCollection<Vector2>(64);

        public readonly ArrayPoolCollection<Vector3> Vector3ArrayPool =
            new ArrayPoolCollection<Vector3>(64);

        public readonly ArrayPoolCollection<Vector4> Vector4ArrayPool =
            new ArrayPoolCollection<Vector4>(64);

        public readonly ArrayPoolCollection<Color32> Color32ArrayPool =
            new ArrayPoolCollection<Color32>(64);

        public readonly ArrayPoolCollection<byte> ByteArrayPool =
            new ArrayPoolCollection<byte>(64);

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder(256);
            sb.ConcatFormat("ChunkPool={0}", ChunkPool.ToString());
            sb.ConcatFormat(",MeshPool={0}", MeshPool.ToString());
            sb.ConcatFormat(",Vec2Arr={0}", Vector2ArrayPool.ToString());
            sb.ConcatFormat(",Vec3Arr={0}", Vector3ArrayPool.ToString());
            sb.ConcatFormat(",Vec4Arr={0}", Vector4ArrayPool.ToString());
            sb.ConcatFormat(",ColorArr={0}", Color32ArrayPool.ToString());
            sb.ConcatFormat(",ByteArr={0}", Color32ArrayPool.ToString());
            return sb.ToString();
        }
    }
}
