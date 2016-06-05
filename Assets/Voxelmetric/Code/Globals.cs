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
                return s_threadPool;
            }
        }

        public static void InitWorkPool()
        {
            if (s_threadPool == null)
            {
                s_threadPool = new ThreadPool();
                s_threadPool.Start();
            }
        }

        // Task pool for IO-related tasks
        private static TaskPool s_IOPool;
        public static TaskPool IOPool
        {
            get
            {
                return s_IOPool;
            }
        }

        public static void InitIOPool()
        {
            if(s_IOPool == null)
            {
                s_IOPool = new TaskPool();
                s_IOPool.Start();
            }
        }

        // Task pool for network-related tasks
        private static TaskPool s_NetworkPool;
        public static TaskPool NetworkPool
        {
            get
            {
                return s_NetworkPool;
            }
        }

        public static void InitNetworkPool()
        {
            if (s_NetworkPool == null)
            {
                s_NetworkPool = new TaskPool();
                s_NetworkPool.Start();
            }
        }

        // Global object pools
        private static GlobalPools s_memPools;
        public static GlobalPools MemPools
        {
            get
            {
                return s_memPools;
            }
        }

        public static void InitMemPools()
        {
            if (s_memPools == null)
                s_memPools = new GlobalPools();
        }
    }
}
