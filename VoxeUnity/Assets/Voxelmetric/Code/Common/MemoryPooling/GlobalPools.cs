using System.Collections.Generic;
using System.Text;
using UnityEngine;
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
        
        public readonly ObjectPool<ThreadPoolItem<SGenerateColliderWorkItem>> GenerateColliderThreadPI =
            new ObjectPool<ThreadPoolItem<SGenerateColliderWorkItem>>(m => new ThreadPoolItem<SGenerateColliderWorkItem>(), 2048, false);

        #endregion

        public readonly ArrayPoolCollection<Vector2> Vector2ArrayPool =
            new ArrayPoolCollection<Vector2>(128);

        public readonly ArrayPoolCollection<Vector3> Vector3ArrayPool =
            new ArrayPoolCollection<Vector3>(128);

        public readonly ArrayPoolCollection<Vector4> Vector4ArrayPool =
            new ArrayPoolCollection<Vector4>(128);

        public readonly ArrayPoolCollection<Color32> Color32ArrayPool =
            new ArrayPoolCollection<Color32>(128);
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("ChunkPool=");
            sb.Append(ChunkPool);
            sb.Append(",MeshPool=");
            sb.Append(MeshPool);
            sb.Append(",Vec2Arr=");
            sb.Append(Vector2ArrayPool);
            sb.Append(",Vec3Arr=");
            sb.Append(Vector3ArrayPool);
            sb.Append(",Vec4Arr=");
            sb.Append(Vector4ArrayPool);
            sb.Append(",ColorArr=");
            sb.Append(Color32ArrayPool);
            return sb.ToString();
        }
    }
}
