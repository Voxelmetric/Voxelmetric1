using System;

namespace Voxelmetric.Code.Common.Threading
{
    public struct TaskPoolItem
    {
        public readonly Action<object> Action;
        public readonly object Arg;

        public TaskPoolItem(Action<object> action, object arg)
        {
            Action = action;
            Arg = arg;
        }
    }
}