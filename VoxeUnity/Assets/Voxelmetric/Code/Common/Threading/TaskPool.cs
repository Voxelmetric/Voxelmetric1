using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using JetBrains.Annotations;
using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public sealed class TaskPool: IDisposable
    {
        //! Each thread contains an object pool
        public LocalPools Pools { get; private set; }

        private List<ITaskPoolItem> m_items; // list of tasks
        private readonly object m_lock = new object();

        private readonly AutoResetEvent m_event; // event for notifing worker thread about work
        private readonly Thread m_thread; // worker thread

        private bool m_stop;

        //! Diagnostics
        private int m_curr, m_max;
        private readonly StringBuilder m_sb = new StringBuilder(32);

        public TaskPool()
        {
            Pools = new LocalPools();

            m_items = new List<ITaskPoolItem>();
            m_event = new AutoResetEvent(false);
            m_thread = new Thread(ThreadFunc)
            {
                IsBackground = true
            };
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

        public int Size
        {
            get { return m_items.Count; }
        }

        public void AddItem(ITaskPoolItem item)
        {
            // Do not add new action in we re stopped or action is invalid
            if (item == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            lock (m_lock)
            {
                m_items.Add(item);
            }
            m_event.Set();
        }

        public void AddItem<T>(Action<T> action) where T: class
        {
            // Do not add new action in we re stopped or action is invalid
            if (action == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            lock (m_lock)
            {
                m_items.Add(new TaskPoolItem<T>(action, null));
            }
            m_event.Set();
        }

        public void AddItem<T>(Action<T> action, T arg)
        {
            // Do not add new action in we re stopped or action is invalid
            if (action == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            lock (m_lock)
            {
                m_items.Add(new TaskPoolItem<T>(action, arg));
            }
            m_event.Set();
        }

        public void AddItemUnsafe([NotNull] ITaskPoolItem item)
        {
            m_items.Add(item);
        }

        public void AddItemUnsafe<T>([NotNull] Action<T> action) where T : class
        {
            // Add task to task list
            m_items.Add(new TaskPoolItem<T>(action, null));
        }

        public void AddItemUnsafe<T>([NotNull] Action<T> action, T arg)
        {
            // Add task to task list
            m_items.Add(new TaskPoolItem<T>(action, arg));
        }

        public void Lock()
        {
            Monitor.Enter(m_lock);
        }

        public void Unlock()
        {
            Monitor.Exit(m_lock);
            m_event.Set();
        }

        private void ThreadFunc()
        {
            var actions = new List<ITaskPoolItem>();

            while (!m_stop)
            {
                // Swap action list pointers
                lock (m_lock)
                {
                    var tmp = m_items;
                    m_items = actions;
                    actions = tmp;
                }

                m_max = actions.Count;

                // Execute all tasks in a row
                for (m_curr = 0; m_curr < actions.Count; m_curr++)
                {
                    // Execute the action
                    // Note, it's up to action to provide exception handling
                    ITaskPoolItem poolItem = actions[m_curr];

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
                        throw;
                    }
#endif
                }
                actions.Clear();
                m_curr = m_max = 0;

                // Wait for next tasks
                m_event.WaitOne();
            }
        }

        public override string ToString()
        {
            return m_sb.Remove(0, m_sb.Length).AppendFormat("{0}/{1}", m_curr, m_max).ToString();
        }
    }
}
