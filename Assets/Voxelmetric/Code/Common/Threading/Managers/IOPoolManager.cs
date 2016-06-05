using System.Collections.Generic;
using UnityEngine;

namespace Assets.Voxelmetric.Code.Common.Threading.Managers
{
    public static class IOPoolManager
    {
        private static readonly List<ThreadItem> WorkItems = new List<ThreadItem>();

        public static void Add(ThreadItem action)
        {
            WorkItems.Add(action);
        }

        public static void Commit()
        {
            // Commit all the work we have
            if (Config.Core.UseMultiThreading)
            {
                TaskPool pool = Globals.IOPool;
                
                for (int i = 0; i<WorkItems.Count; i++)
                {
                    var item = WorkItems[i];
                    pool.AddItem(item.Action, item.Arg);
                }

                //Debug.Log("IOPool tasks: " + pool.Size);
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
