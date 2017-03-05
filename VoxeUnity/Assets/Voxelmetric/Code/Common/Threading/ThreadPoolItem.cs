using System;

namespace Voxelmetric.Code.Common.Threading
{
    public interface AThreadPoolItem: ITaskPoolItem
    {
        int ThreadID { get; }
        long Time { get; }
    }

    public class ThreadPoolItem<T>: AThreadPoolItem
    {
        private Action<T> Action;
        private T Arg;

        public int ThreadID { get; private set; }

        public long Time { get; private set; }

        public ThreadPoolItem()
        {
        }

        public ThreadPoolItem(ThreadPool pool, Action<T> action, T arg, long time = long.MaxValue)
        {
            Action = action;
            Arg = arg;
            ThreadID = pool.GenerateThreadID();
            Time = time;
        }

        public ThreadPoolItem(int threadID, Action<T> action, T arg, long time = long.MaxValue)
        {
            Action = action;
            Arg = arg;
            ThreadID = threadID;
            Time = time;
        }

        public void Set(ThreadPool pool, Action<T> action, T arg, long time = long.MaxValue)
        {
            Action = action;
            Arg = arg;
            ThreadID = pool.GenerateThreadID();
            Time = time;
        }

        public void Set(int threadID, Action<T> action, T arg, long time = long.MaxValue)
        {
            Action = action;
            Arg = arg;
            ThreadID = threadID;
            Time = time;
        }

        public void Run()
        {
            Action(Arg);
        }
    }
}