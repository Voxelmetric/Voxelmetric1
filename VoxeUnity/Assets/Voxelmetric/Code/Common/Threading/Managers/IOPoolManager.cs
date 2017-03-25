using System.Collections.Generic;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class IOPoolManager
    {
        private static readonly List<ITaskPoolItem> WorkItems = new List<ITaskPoolItem>(2048);

        private static readonly TimeBudgetHandler TimeBudget = Features.UseThreadedIO
                                                                   ? null
                                                                   : new TimeBudgetHandler(10);

        public static void Add(ITaskPoolItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            if (WorkItems.Count<=0)
                return;

            // Commit all the work we have
            if (Features.UseThreadedIO)
            {
                TaskPool pool = Globals.IOPool;

                for (int i = 0; i<WorkItems.Count; i++)
                {
                    pool.AddItem(WorkItems[i]);
                }
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
            return Features.UseThreadedIO ? Globals.IOPool.ToString() : WorkItems.Count.ToString();
        }
    }
}