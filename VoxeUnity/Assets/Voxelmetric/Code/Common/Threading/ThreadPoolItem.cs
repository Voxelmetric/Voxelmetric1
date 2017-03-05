using System;

namespace Voxelmetric.Code.Common.Threading
{
    public interface AThreadPoolItem: ITaskPoolItem
    {
        int ThreadID { get; }
        long Time { get; }
    }

    public struct ThreadPoolItem<T>: AThreadPoolItem
    {
        private readonly ITaskPoolItem m_item;

        public int ThreadID { get; private set; }

        public long Time { get; private set; }

        public ThreadPoolItem(ThreadPool pool, ITaskPoolItem item, long time = long.MaxValue): this()
        {
            m_item = item;
            ThreadID = pool.GenerateThreadID();
            Time = time;
        }

        public ThreadPoolItem(int threadID, ITaskPoolItem item, long time = long.MaxValue): this()
        {
            m_item = item;
            ThreadID = threadID;
            Time = time;
        }

        public ThreadPoolItem(ThreadPool pool, Action<T> action, T arg, long time = long.MaxValue): this()
        {
            m_item = new TaskPoolItem<T>(action, arg);
            ThreadID = pool.GenerateThreadID();
            Time = time;
        }

        public ThreadPoolItem(int threadID, Action<T> action, T arg, long time = long.MaxValue): this()
        {
            m_item = new TaskPoolItem<T>(action, arg);
            ThreadID = threadID;
            Time = time;
        }

        public void Run()
        {
            m_item.Run();
        }
    }
}