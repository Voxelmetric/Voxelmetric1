using System;

namespace Voxelmetric.Code.Common.Threading
{
    public struct ThreadItem
    {
        public readonly int ThreadID;
        public readonly Action<object> Action;
        public readonly object Arg;

        public ThreadItem(Action<object> action, object arg)
        {
            ThreadID = -1;
            Action = action;
            Arg = arg;
        }

        public ThreadItem(int threadID, Action<object> action, object arg)
        {
            ThreadID = threadID;
            Action = action;
            Arg = arg;
        }
    }
}