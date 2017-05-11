using System.Diagnostics;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Builders.Collider;
using Voxelmetric.Code.Builders.Geometry;
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

        // Geometry mesh builder
        private static readonly IMeshBuilder s_cubeMeshBuilder = new CubeMeshBuilder();
        public static IMeshBuilder CubeMeshBuilder
        {
            get
            {
                return s_cubeMeshBuilder;
            }
        }

        // Collider mesh builder
        private static readonly IMeshBuilder s_cubeMeshColliderBuilder = new CubeMeshColliderBuilder();
        public static IMeshBuilder CubeMeshColliderBuilder
        {
            get
            {
                return s_cubeMeshColliderBuilder;
            }
        }

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
        private static readonly TimeBudgetHandler s_geometryBudget = new TimeBudgetHandler(4); // 4 ms a frame for geometry generation
        public static TimeBudgetHandler GeometryBudget {
            get
            {
                return s_geometryBudget;
            }
        }

        private static readonly TimeBudgetHandler s_edgeSyncBudget = new TimeBudgetHandler(4); // 4 ms a frame for edge synchronization
        public static TimeBudgetHandler EdgeSyncBudget {
            get
            {
                return s_edgeSyncBudget;
            }
        }

        private static readonly TimeBudgetHandler s_setBlockBudget = new TimeBudgetHandler(4); // 4 ms a frame for edge synchronization
        public static TimeBudgetHandler SetBlockBudget
        {
            get
            {
                return s_setBlockBudget;
            }
        }
    }
}
