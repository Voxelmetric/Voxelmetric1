using System.Diagnostics;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code
{
    public static class Globals
    {
        // Thread pool
        public static ThreadPool WorkPool { get; private set; }

        public static void InitWorkPool()
        {
            if (WorkPool == null)
            {
                WorkPool = new ThreadPool();
                WorkPool.Start();
            }
        }

        // Task pool for IO-related tasks
        public static TaskPool IOPool { get; private set; }

        public static void InitIOPool()
        {
            if(IOPool == null)
            {
                IOPool = new TaskPool();
                IOPool.Start();
            }
        }

        // Geometry mesh builder used for the terrain
        public static AMeshBuilder ModelMeshBuilder { get; } = new CubeMeshBuilder(Env.BlockSize, Env.ChunkSize) { SideMask = 0 };

        // Geometry mesh builder used for the terrain
        public static AMeshBuilder TerrainMeshBuilder { get; } = new CubeMeshBuilder(Env.BlockSize, Env.ChunkSize) { SideMask = Features.DontRenderWorldEdgesMask };

        // Collider mesh builder used for the terrain
        public static AMeshBuilder TerrainMeshColliderBuilder { get; } = new CubeMeshColliderBuilder(Env.BlockSize, Env.ChunkSize) { SideMask = Features.DontRenderWorldEdgesMask };

        // Global object pools
        public static GlobalPools MemPools { get; private set; }

        public static void InitMemPools()
        {
            if (MemPools == null)
                MemPools = new GlobalPools();
        }

        // Global stop watch
        public static Stopwatch Watch { get; private set; }
        public static void InitWatch()
        {
            if (Watch==null)
            {
                Watch = new Stopwatch();
                Watch.Start();
            }
        }

        // Global time budget handlers
        public static TimeBudgetHandler GeometryBudget { get; } = new TimeBudgetHandler(4);

        public static TimeBudgetHandler EdgeSyncBudget { get; } = new TimeBudgetHandler(4);

        public static TimeBudgetHandler SetBlockBudget { get; } = new TimeBudgetHandler(4);
    }
}
