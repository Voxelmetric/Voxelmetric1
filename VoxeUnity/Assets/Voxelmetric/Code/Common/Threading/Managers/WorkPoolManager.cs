using System.Collections.Generic;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class WorkPoolManager
    {
        private static readonly List<AThreadPoolItem> WorkItems = new List<AThreadPoolItem>(2048);

        private static readonly TimeBudgetHandler TimeBudget = Utilities.Core.UseThreadPool
                                                                   ? null
                                                                   : new TimeBudgetHandler(10);

        public static void Add(AThreadPoolItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            if (WorkItems.Count<=0)
                return;

            // Commit all the work we have
            if (Utilities.Core.UseThreadPool)
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
                    AThreadPoolItem curr = WorkItems[i];
                    AThreadPoolItem next = WorkItems[i+1];
                    if (curr.ThreadID==next.ThreadID)
                    {
                        to = i+1;
                        continue;
                    }

                    tp = pool.GetTaskPool(curr.ThreadID);
                    tp.Lock();
                    for (int j = from; j<=to; j++)
                    {
                        tp.AddItemUnsafe(WorkItems[j]);
                    }
                    tp.Unlock();

                    from = i+1;
                    to = from;
                }

                tp = pool.GetTaskPool(WorkItems[from].ThreadID);
                tp.Lock();
                for (int j = from; j<=to; j++)
                {
                    tp.AddItemUnsafe(WorkItems[j]);
                }
                tp.Unlock();


            }
            else
            {
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    TimeBudget.StartMeasurement();
                    WorkItems[i].Run();
                    TimeBudget.StopMeasurement();

                    // If the tasks take too much time to finish, spread them out over multiple
                    // frames to avoid performance spikes
                    if (!TimeBudget.HasTimeBudget)
                    {
                        WorkItems.RemoveRange(0, i+1);
                        return;
                    }
                }
            }

            // Remove processed work items
            WorkItems.Clear();
        }

        public new static string ToString()
        {
            return Utilities.Core.UseThreadPool ? Globals.WorkPool.ToString() : WorkItems.Count.ToString();
        }
    }
}