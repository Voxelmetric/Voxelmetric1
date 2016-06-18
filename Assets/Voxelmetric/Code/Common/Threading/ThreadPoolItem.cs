using System;

namespace Voxelmetric.Code.Common.Threading
{
    public struct ThreadPoolItem
    {
        public readonly int ThreadID;
        public readonly Action<object> Action;
        public readonly object Arg;
        public readonly float Time;

        public ThreadPoolItem(ThreadPool pool, Action<object> action, object arg, float time = float.MaxValue)
        {
            ThreadID = pool.GenerateThreadID();
            Action = action;
            Arg = arg;
            Time = time;
        }

        public ThreadPoolItem(int threadID, Action<object> action, object arg, float time = float.MaxValue)
        {
            ThreadID = threadID;
            Action = action;
            Arg = arg;
            Time = time;
        }
    }
}