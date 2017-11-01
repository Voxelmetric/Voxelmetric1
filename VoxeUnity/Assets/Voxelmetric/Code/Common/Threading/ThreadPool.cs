using System;
using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Common.Threading
{
    public class ThreadPool
    {
        private bool m_started;
        private volatile int m_nextThreadIndex = 0;

        //! Threads used by thread pool
        private readonly TaskPool[] m_pools;

        //! Diagnostics
        private readonly StringBuilder m_sb = new StringBuilder(128);

        public ThreadPool()
        {
            m_started = false;

            // If the number of threads is not correctly specified, create as many as possible minus one (taking
            // all available core is not effective - there's still the main thread we should not forget).
            // Allways create at least one thread, however.
            int threadCnt = Features.UseThreadPool ? Mathf.Max(Environment.ProcessorCount-1, 1) : 1;
            m_pools = Helpers.CreateArray1D<TaskPool>(threadCnt);
            // NOTE: Normally, I would simply call CreateAndInitArray1D, however, any attempt to allocate memory
            // for TaskPool in this contructor ends up with Unity3D crashing :(
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

        public int Size
        {
            get { return m_pools.Length; }
        }

        public override string ToString()
        {
            m_sb.Length = 0;
            for (int i = 0; i<m_pools.Length-1; i++)
                m_sb.ConcatFormat("{0}, ", m_pools[i].ToString());
            return m_sb.ConcatFormat("{0}", m_pools[m_pools.Length-1].ToString()).ToString();
        }
    }
}
