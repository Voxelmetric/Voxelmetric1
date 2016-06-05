using Assets.Voxelmetric.Code.Common.MemoryPooling;
using Assets.Voxelmetric.Code.Common.Threading;

namespace Assets.Voxelmetric.Code
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

        // Task pool for network-related tasks
        public static TaskPool NetworkPool { get; private set; }

        public static void InitNetworkPool()
        {
            if (NetworkPool == null)
            {
                NetworkPool = new TaskPool();
                NetworkPool.Start();
            }
        }

        // Global object pools
        public static GlobalPools MemPools { get; private set; }

        public static void InitMemPools()
        {
            if (MemPools == null)
                MemPools = new GlobalPools();
        }
    }
}
