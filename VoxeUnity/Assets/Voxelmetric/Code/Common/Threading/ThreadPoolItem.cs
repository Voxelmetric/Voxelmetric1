using System;

namespace Voxelmetric.Code.Common.Threading
{
    public abstract class AThreadPoolItem: ITaskPoolItem
    {
        public readonly int ThreadID;
        public readonly long Time;

        protected AThreadPoolItem(ThreadPool pool, long time)
        {
            ThreadID = pool.GenerateThreadID();
            Time = time;
        }

        protected AThreadPoolItem(int threadID, long time)
        {
            ThreadID = threadID;
            Time = time;
        }

        public abstract void Run();
    }

    public class AThreadPoolItem<T>: AThreadPoolItem
    {
        public readonly Action<T> Action;
        public readonly T Arg;
        

        public AThreadPoolItem(ThreadPool pool, Action<T> action, T arg, long time = long.MaxValue) :
            base(pool, time)
        {
            Action = action;
            Arg = arg;
        }

        public AThreadPoolItem(int threadID, Action<T> action, T arg, long time = long.MaxValue) :
            base(threadID, time)
        {
            Action = action;
            Arg = arg;
        }

        public override void Run()
        {
            Action(Arg);
        }
    }
}