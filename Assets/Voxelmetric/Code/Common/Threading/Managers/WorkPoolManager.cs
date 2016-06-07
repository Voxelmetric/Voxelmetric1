using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Threading.Managers
{
    public static class WorkPoolManager
    {
        private static readonly List<ThreadItem> WorkItems = new List<ThreadItem>();

        public static void Add(ThreadItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (Utilities.Core.UseMultiThreading)
            {
                ThreadPool pool = Globals.WorkPool;

                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    if(item.ThreadID>=0)
                        pool.AddItem(item.ThreadID, item.Action, item.Arg);
                    else
                        pool.AddItem(item.Action, item.Arg);
                }

                //Debug.Log("WorkPool tasks: " + pool.Size);
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
