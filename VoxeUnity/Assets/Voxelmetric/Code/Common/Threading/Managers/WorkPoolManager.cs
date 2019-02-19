using System.Collections.Generic;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class WorkPoolManager
    {
        private static readonly List<AThreadPoolItem> WorkItems = new List<AThreadPoolItem>(2048);
        private static readonly List<AThreadPoolItem> WorkItemsP = new List<AThreadPoolItem>(2048);

        private static readonly HashSet<TaskPool> Threads = Features.UseThreadPool
                                                           ? new HashSet<TaskPool>()
                                                           : null;
        private static readonly List<TaskPool> ThreadsIter = Features.UseThreadPool
                                                            ? new List<TaskPool>()
                                                            : null;

        private static readonly TimeBudgetHandler TimeBudget = Features.UseThreadPool
                                                                   ? null
                                                                   : new TimeBudgetHandler(10);

        public static void Add(AThreadPoolItem action, bool priority)
        {
            if (priority)
                WorkItemsP.Add(action);
            else
                WorkItems.Add(action);
        }

        private static void ProcessWorkItems(List<AThreadPoolItem> wi)
        {
            // Skip empty lists
            if (wi.Count<=0)
                return;

            // Sort our work items by threadID
            wi.Sort((x, y) => x.ThreadID.CompareTo(y.ThreadID));

            ThreadPool pool = Globals.WorkPool;
            int from = 0;
            int to = 0;

            // Commit items to their respective task thread.
            // Instead of commiting tasks one by one we commit them all at once
            TaskPool tp;
            for (int i = 0; i < wi.Count - 1; i++)
            {
                AThreadPoolItem curr = wi[i];
                AThreadPoolItem next = wi[i + 1];
                if (curr.ThreadID == next.ThreadID)
                {
                    to = i + 1;
                    continue;
                }

                tp = pool.GetTaskPool(curr.ThreadID);
                for (int j = from; j <= to; j++)
                {
                    tp.AddPriorityItem(wi[j]);
                }
                if (Threads.Add(tp))
                    ThreadsIter.Add(tp);

                from = i + 1;
                to = from;
            }

            tp = pool.GetTaskPool(wi[from].ThreadID);
            for (int j = from; j <= to; j++)
            {
                tp.AddPriorityItem(wi[j]);
            }
            if (Threads.Add(tp))
                ThreadsIter.Add(tp);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (Features.UseThreadPool)
            {
                // Priority tasks first
                ProcessWorkItems(WorkItemsP);
                // Oridinary tasks second
                ProcessWorkItems(WorkItems);

                // Commit all tasks we collected to their respective threads
                for (int i = 0; i<ThreadsIter.Count; i++)
                    ThreadsIter[i].Commit();

                Threads.Clear();
                ThreadsIter.Clear();
            }
            else
            {
                WorkItemsP.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                for (int i = 0; i < WorkItemsP.Count; i++)
                {
                    TimeBudget.StartMeasurement();
                    WorkItemsP[i].Run();
                    TimeBudget.StopMeasurement();

                    // If the tasks take too much time to finish, spread them out over multiple
                    // frames to avoid performance spikes
                    if (!TimeBudget.HasTimeBudget)
                    {
                        WorkItemsP.RemoveRange(0, i + 1);
                        return;
                    }
                }

                WorkItems.Sort((x, y) => x.Priority.CompareTo(y.Priority));
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
            WorkItemsP.Clear();
        }

        public new static string ToString()
        {
            return Features.UseThreadPool ? Globals.WorkPool.ToString() : WorkItems.Count.ToString();
        }
    }
}