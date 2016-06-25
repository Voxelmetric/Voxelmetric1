using System;
using System.Collections.Generic;
using System.Threading;
using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public sealed class TaskPool: IDisposable
    {
        //! Each thread contains an object pool
        public LocalPools Pools { get; private set; }

        private List<TaskPoolItem> m_items; // list of tasks
        private readonly object m_lock = new object();

        private readonly AutoResetEvent m_event; // event for notifing worker thread about work
        private readonly Thread m_thread; // worker thread

        private bool m_stop;

        public TaskPool()
        {
            Pools = new LocalPools();

            m_items = new List<TaskPoolItem>();
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

        public void AddItem(Action<object> action)
        {
            // Do not add new action in we re stopped or action is invalid
            if (action == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            lock (m_lock)
            {
                m_items.Add(new TaskPoolItem(action, null));
            }
            m_event.Set();
        }

        public void AddItem(Action<object> action, object arg)
        {
            // Do not add new action in we re stopped or action is invalid
            if (action == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            lock (m_lock)
            {
                m_items.Add(new TaskPoolItem(action, arg));
            }
            m_event.Set();
        }

        public void AddItemUnsafe(Action<object> action)
        {
            // Do not add new action in we re stopped or action is invalid
            if (action == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            m_items.Add(new TaskPoolItem(action, null));
        }

        public void AddItemUnsafe(Action<object> action, object arg)
        {
            // Do not add new action in we re stopped or action is invalid
            if (action == null || m_stop)
                return;

            // Add task to task list and notify the worker thread
            m_items.Add(new TaskPoolItem(action, arg));
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
            var actions = new List<TaskPoolItem>();

            while (!m_stop)
            {
                // Swap action list pointers
                lock (m_lock)
                {
                    var tmp = m_items;
                    m_items = actions;
                    actions = tmp;
                }


                // Execute all tasks in a row
                for (int i = 0; i < actions.Count; i++)
                {
                    // Execute the action
                    // Note, it's up to action to provide exception handling
                    TaskPoolItem poolItem = actions[i];

#if DEBUG
                    try
                    {
#endif
                        poolItem.Action(poolItem.Arg);
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

                // Wait for next tasks
                m_event.WaitOne();
            }
        }
    }
}
