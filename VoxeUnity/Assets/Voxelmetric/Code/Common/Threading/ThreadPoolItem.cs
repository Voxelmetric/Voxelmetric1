using System;

namespace Voxelmetric.Code.Common.Threading
{
    public interface AThreadPoolItem: ITaskPoolItem
    {
        int ThreadID { get; }
    }

    public class ThreadPoolItem<T>: AThreadPoolItem
    {
        private Action<T> m_action;
        private T m_arg;
        private bool m_processed = false;

        public int ThreadID { get; private set; }
        public long Priority { get; private set; }

        public ThreadPoolItem()
        {
        }

        public ThreadPoolItem(ThreadPool pool, Action<T> action, T arg, long priority = long.MinValue)
        {
            m_action = action;
            m_arg = arg;
            ThreadID = pool.GenerateThreadID();
            Priority = priority;
        }

        public ThreadPoolItem(int threadID, Action<T> action, T arg, long time = long.MinValue)
        {
            m_action = action;
            m_arg = arg;
            ThreadID = threadID;
            Priority = time;
        }

        public void Set(ThreadPool pool, Action<T> action, T arg, long time = long.MinValue)
        {
            m_action = action;
            m_arg = arg;
            m_processed = false;
            ThreadID = pool.GenerateThreadID();
            Priority = time;
        }

        public void Set(int threadID, Action<T> action, T arg, long time = long.MaxValue)
        {
            m_action = action;
            m_arg = arg;
            m_processed = false;
            ThreadID = threadID;
            Priority = time;
        }

        public void Run()
        {
            if (!m_processed)
            {
                m_action(m_arg);
                m_processed = true;
            }
        }
    }
}