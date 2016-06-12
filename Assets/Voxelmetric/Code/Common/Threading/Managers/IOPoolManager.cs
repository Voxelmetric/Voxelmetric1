using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class IOPoolManager
    {
        private static readonly List<TaskPoolItem> WorkItems = new List<TaskPoolItem>();

        public static void Add(TaskPoolItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (Utilities.Core.UseMultiThreading)
            {
                TaskPool pool = Globals.IOPool;
                
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    pool.AddItem(item.Action, item.Arg);
                }
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
