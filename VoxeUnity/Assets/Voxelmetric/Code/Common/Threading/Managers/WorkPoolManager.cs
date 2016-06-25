using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class WorkPoolManager
    {
        private static readonly List<ThreadPoolItem> WorkItems = new List<ThreadPoolItem>();

        public static void Add(ThreadPoolItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            if (WorkItems.Count<=0)
                return;

            // Commit all the work we have
            if (Utilities.Core.UseMultiThreading)
            {
                ThreadPool pool = Globals.WorkPool;
                
                // Sort our work items by threadID
                WorkItems.Sort(
                    (x, y) =>
                    {
                        int ret = x.ThreadID.CompareTo(y.ThreadID);
                        if (ret==0)
                            ret = x.Time.CompareTo(y.Time);
                        return ret;
                    });

                // Commit items to their respective task thread.
                // Instead of commiting tasks one by one, we take them all and commit
                // them at once
                TaskPool tp;
                int from = 0, to = 0;
                for (int i = 0; i<WorkItems.Count-1; i++)
                {
                    ThreadPoolItem curr = WorkItems[i];
                    ThreadPoolItem next = WorkItems[i+1];
                    if (curr.ThreadID==next.ThreadID)
                    {
                        to = i+1;
                        continue;
                    }

                    tp = pool.GetTaskPool(curr.ThreadID);
                    tp.Lock();
                    for (int j = from; j<=to; j++)
                    {
                        ThreadPoolItem item = WorkItems[j];
                        tp.AddItemUnsafe(item.Action, item.Arg);
                    }
                    tp.Unlock();

                    from = i+1;
                    to = from;
                }
                    
                tp = pool.GetTaskPool(WorkItems[from].ThreadID);
                tp.Lock();
                for (int j = from; j<=to; j++)
                {
                    ThreadPoolItem item = WorkItems[j];
                    tp.AddItemUnsafe(item.Action, item.Arg);
                }
                tp.Unlock();
            }
            else
            {
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    item.Action(item.Arg);
                }
            }
            
            // Remove processed work items
            WorkItems.Clear();
        }
    }  
}
