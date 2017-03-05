using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class IOPoolManager
    {
        private static readonly List<ITaskPoolItem> WorkItems = new List<ITaskPoolItem>(512);

        public static void Add(ITaskPoolItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (Utilities.Core.UseThreadedIO)
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
                    WorkItems[i].Run();
                }
            }

            // Remove processed work items
            WorkItems.Clear();
        }
    }
}
