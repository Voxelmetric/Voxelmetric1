using System;

namespace Voxelmetric.Code.Common.Threading
{
    public interface ITaskPoolItem
    {
        void Run();
    }

    public class TaskPoolItem<T>: ITaskPoolItem
    {
        private Action<T> Action;
        private T Arg;

        public TaskPoolItem()
        {
        }

        public TaskPoolItem(Action<T> action, T arg)
        {
            Action = action;
            Arg = arg;
        }

        public void Set(Action<T> action, T arg)
        {
            Action = action;
            Arg = arg;
        }

        public void Run()
        {
            Action(Arg);
        }
    }
}