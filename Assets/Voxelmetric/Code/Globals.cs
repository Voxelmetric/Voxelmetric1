using Assets.Voxelmetric.Code.Common.MemoryPooling;
using Assets.Voxelmetric.Code.Common.Threading;

namespace Assets.Voxelmetric.Code
{
    public static class Globals
    {
        // Thread pool
        private static ThreadPool s_threadPool;
        public static ThreadPool WorkPool
        {
            get
            {
                if (s_threadPool == null)
                {
                    s_threadPool = new ThreadPool();
                    s_threadPool.Start();
                }
                return s_threadPool;
            }
        }

        // Task pool for IO-related tasks
        private static TaskPool s_IOPool;
        public static TaskPool IOPool
        {
            get
            {
                if (s_IOPool == null)
                {
                    s_IOPool = new TaskPool();
                    s_IOPool.Start();
                }
                return s_IOPool;
            }
        }

        // Global object pools
        private static GlobalPools s_pools;
        public static GlobalPools Pools
        {
            get
            {
                return s_pools;
            }
        }

        public static void InitPools()
        {
            if (s_pools == null)
                s_pools = new GlobalPools();
        }
    }
}
