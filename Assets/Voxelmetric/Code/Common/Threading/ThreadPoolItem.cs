using System;

namespace Voxelmetric.Code.Common.Threading
{
    public struct ThreadPoolItem
    {
        public readonly int ThreadID;
        public readonly Action<object> Action;
        public readonly object Arg;

        public ThreadPoolItem(ThreadPool pool, Action<object> action, object arg)
        {
            ThreadID = pool.GenerateThreadID();
            Action = action;
            Arg = arg;
        }

        public ThreadPoolItem(int threadID, Action<object> action, object arg)
        {
            ThreadID = threadID;
            Action = action;
            Arg = arg;
        }
    }
}