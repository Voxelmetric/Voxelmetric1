using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using NUnit.Framework;
using UnityEngine;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public sealed class TaskPool: IDisposable
    {
        //! Each thread contains an object pool
        public LocalPools Pools { get; }

        private readonly object m_lock = new object();

        private List<ITaskPoolItem> m_items; // list of tasks
        private List<ITaskPoolItem> m_itemsP; // list of tasks

        private readonly List<ITaskPoolItem> m_itemsTmp; // temporary list of tasks
        private readonly List<ITaskPoolItem> m_itemsTmpP; // temporary list of tasks

        private readonly AutoResetEvent m_event; // event for notifing worker thread about work
        private readonly Thread m_thread; // worker thread

        private bool m_stop;
        private bool m_hasPriorityItems;

        //! Diagnostics
        private int m_curr, m_max, m_currP, m_maxP;
        private readonly StringBuilder m_sb = new StringBuilder(32);

        public TaskPool()
        {
            Pools = new LocalPools();
            
            m_items = new List<ITaskPoolItem>();
            m_itemsP = new List<ITaskPoolItem>();

            m_itemsTmp = new List<ITaskPoolItem>();
            m_itemsTmpP = new List<ITaskPoolItem>();

            m_event = new AutoResetEvent(false);
            m_thread = new Thread(ThreadFunc)
            {
                IsBackground = true
            };
            
            m_stop = false;
            m_hasPriorityItems = false;

            m_curr = m_max = m_currP = m_maxP = 0;
        }

        ~TaskPool()
        {
            Dispose(false);
        }

        private void Dispose(bool disposing)
        {
            Stop();

            if (disposing)
            {
                // dispose managed resources
                m_event.Close();
            }
            // free native resources
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        public void Start()
        {
            m_thread.Start();
        }

        public void Stop()
        {
            m_stop = true;
            m_event.Set();
        }
        
        public void AddItem(ITaskPoolItem item)
        {
            Assert.IsNotNull(item);
            m_itemsTmp.Add(item);
        }

        public void AddItem<T>(Action<T> action, long priority = long.MinValue) where T : class
        {
            Assert.IsNotNull(action);
            m_itemsTmp.Add(new TaskPoolItem<T>(action, null, priority));
        }

        public void AddItem<T>(Action<T> action, T arg, long time = long.MinValue)
        {
            Assert.IsNotNull(action);
            m_itemsTmp.Add(new TaskPoolItem<T>(action, arg, time));
        }

        public void AddPriorityItem(ITaskPoolItem item)
        {
            Assert.IsNotNull(item);
            m_itemsTmpP.Add(item);
        }

        public void AddPriorityItem<T>(Action<T> action, long priority = long.MinValue) where T : class
        {
            Assert.IsNotNull(action);
            m_itemsTmpP.Add(new TaskPoolItem<T>(action, null, priority));
        }

        public void AddPriorityItem<T>(Action<T> action, T arg, long priority = long.MinValue)
        {
            Assert.IsNotNull(action);
            m_itemsTmpP.Add(new TaskPoolItem<T>(action, arg, priority));
        }

        public void Commit()
        {
            if (m_itemsTmp.Count<=0 && m_itemsTmpP.Count<=0)
                return;

            lock (m_lock)
            {
                m_items.AddRange(m_itemsTmp);
                m_itemsP.AddRange(m_itemsTmpP);

                m_hasPriorityItems = m_itemsP.Count > 0;
            }

            m_itemsTmp.Clear();
            m_itemsTmpP.Clear();

            m_event.Set();
        }

        private void ThreadFunc()
        {
            var actions = new List<ITaskPoolItem>();
            var actionsP = new List<ITaskPoolItem>();

            while (!m_stop)
            {
                // Swap action list pointers
                lock (m_lock)
                {
                    var tmp = actions;
                    actions = m_items;
                    m_items = tmp;

                    tmp = actionsP;
                    actionsP = m_itemsP;
                    m_itemsP = tmp;

                    m_hasPriorityItems = false;
                }

                // Sort tasks by priority
                actions.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                m_max = actions.Count;
                m_curr = 0;

            priorityLabel:
                actionsP.Sort((x, y) => x.Priority.CompareTo(y.Priority));
                m_maxP = actionsP.Count;
                m_currP = 0;

                // Process priority tasks first
                for (; m_currP< m_maxP; m_currP++)
                {
                    var poolItem = actionsP[m_currP];

#if DEBUG
                    try
                    {
#endif
                        poolItem.Run();
#if DEBUG
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
                }
#endif
                
                // Process ordinary tasks now
                for (; m_curr < m_max; m_curr++)
                {
                    var poolItem = actions[m_curr];

#if DEBUG
                    try
                    {
#endif
                        poolItem.Run();
#if DEBUG
                    }
                    catch (Exception ex)
                    {
                        Debug.LogException(ex);
                    }
#endif

                    // Let's see if there wasn't a priority action queued in the meantime.
                    // No need to lock these bool variables here. If they're not set yet,
                    // we'll simply read their state in the next iteration
                    if (!m_stop && m_hasPriorityItems)
                    {
                        lock (m_lock)
                        {
                            actionsP.AddRange(m_itemsP);
                            m_itemsP.Clear();

                            m_hasPriorityItems = false;
                            goto priorityLabel;
                        }
                        
                    }
                }

                // Everything processed
                actions.Clear();
                actionsP.Clear();
                m_curr = m_max = m_currP = m_maxP = 0;

                // Wait for new tasks
                m_event.WaitOne();
            }
        }

        public override string ToString()
        {
            m_sb.Length = 0;
            return m_sb.ConcatFormat("{0}/{1}, prio:{2}/{3}", m_curr, m_max, m_currP, m_maxP).ToString();
        }
    }
}
