using System;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public class ThreadPool
    {
        private bool m_started;
        private volatile int m_nextThreadIndex = 0;

        //! Threads used by thread pool
        private readonly TaskPool[] m_pools;

        public ThreadPool()
        {
            m_started = false;

            // If the number of threads is not correctly specified, create as many as possible minus one (taking
            // all available core is not effective - there's still the main thread we should not forget).
            // Allways create at least one thread, however.
            int threadCnt = Mathf.Max(Environment.ProcessorCount-1, 1);
            m_pools = Helpers.CreateArray1D<TaskPool>(threadCnt);
            // NOTE: Normally, I would simply call CreateAndInitArray1D, however, any attempt to allocate memory
            // for TaskPool in this contructor ends up with Unity3D crashing :(

            //Debug.Log("Threadpool created with " + threadCnt + " threads");
        }

        public int GenerateThreadID()
        {
            m_nextThreadIndex = GetThreadIDFromIndex(m_nextThreadIndex + 1);
            return m_nextThreadIndex;
        }

        public int GetThreadIDFromIndex(int index)
        {
            return Helpers.Mod(index, m_pools.Length);
        }

        public LocalPools GetPool(int index)
        {
            int id = GetThreadIDFromIndex(index);
            return m_pools[id].Pools;
        }

        public TaskPool GetTaskPool(int index)
        {
            return m_pools[index];
        }

        public void Start()
        {
            if (m_started)
                return;
            m_started = true;

            for (int i = 0; i<m_pools.Length; i++)
            {
                m_pools[i] = new TaskPool();
                m_pools[i].Start();
            }
        }

        public void AddItem(Action<object> action)
        {
            int threadID = GenerateThreadID();
            m_pools[threadID].AddItem(action);
        }

        public void AddItem(int threadID, Action<object> action)
        {
            // Assume a proper index is passed as an arugment
            Assert.IsTrue(threadID>=0 && threadID<m_pools.Length);
            m_pools[threadID].AddItem(action);
        }

        public void AddItem(Action<object> action, object arg)
        {
            int threadID = GenerateThreadID();
            m_pools[threadID].AddItem(action, arg);
        }

        public void AddItem(int threadID, Action<object> action, object arg)
        {
            // Assume a proper index is passed as an arugment
            Assert.IsTrue(threadID>=0 && threadID<m_pools.Length);
            m_pools[threadID].AddItem(action, arg);
        }
        
        public int Size
        {
            get
            {
                int items = 0;
                for (int i = 0; i<m_pools.Length; i++)
                    items += m_pools[i].Size;
                return items;
            }
        }
    }
}
