using System;

namespace Voxelmetric.Code.Common.Threading
{
    public interface ITaskPoolItem
    {
        long Priority { get; }
        void Run();
    }

    public class TaskPoolItem<T>: ITaskPoolItem
    {
        private Action<T> m_action;
        private T m_arg;
        private bool m_processed = false;

        public long Priority { get; private set; }

        public TaskPoolItem()
        {
        }

        public TaskPoolItem(Action<T> action, T arg, long priority = long.MinValue)
        {
            m_action = action;
            m_arg = arg;
            Priority = priority;
        }

        public void Set(Action<T> action, T arg, long priority = long.MinValue)
        {
            m_action = action;
            m_arg = arg;
            Priority = priority;
            m_processed = false;
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