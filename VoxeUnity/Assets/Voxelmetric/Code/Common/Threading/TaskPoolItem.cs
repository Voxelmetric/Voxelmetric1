using System;

namespace Voxelmetric.Code.Common.Threading
{
    public interface ITaskPoolItem
    {
        void Run();
    }

    public struct TaskPoolItem<T>: ITaskPoolItem
    {
        public readonly Action<T> Action;
        public readonly T Arg;

        public TaskPoolItem(Action<T> action, T arg)
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