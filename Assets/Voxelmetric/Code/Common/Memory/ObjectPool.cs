using System;
using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Memory
{
    public sealed class ObjectPool<T> where T: class
    {
        //! Delegate handling allocation of memory
        private readonly ObjectPoolAllocator<T> m_objectAllocator;
        //! Delegate handling releasing of memory
        private readonly Action<T> m_objectDeallocator;
        //! Object storage
        private readonly List<T> m_objects;
        //! Index to the first available object in object pool
        private int m_objectIndex;
        //! Initial size of object pool. We never deallocate memory under this threshold
        private readonly int m_initialSize;
        //! If true object pool will try to release some of the unused memory if the difference in currently used size and capacity of pool is too big
        private readonly bool m_autoReleaseMemory;

        public int Capacity {
            get { return m_objects.Count; }
        }

        public ObjectPool(Func<T, T> objectAllocator, int initialSize, bool autoReleaseMememory)
        {
            m_objectAllocator = new ObjectPoolAllocator<T>(objectAllocator);
            m_objectDeallocator = null;
            m_initialSize = initialSize;
            m_autoReleaseMemory = autoReleaseMememory;
            m_objectIndex = 0;

            m_objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
                m_objects.Add(m_objectAllocator.Action(m_objectAllocator.Arg));
        }

        public ObjectPool(ObjectPoolAllocator<T> objectAllocator, int initialSize, bool autoReleaseMememory)
        {
            m_objectAllocator = objectAllocator;
            m_objectDeallocator = null;
            m_initialSize = initialSize;
            m_autoReleaseMemory = autoReleaseMememory;
            m_objectIndex = 0;

            m_objects = new List<T>(initialSize);
            for (int i=0; i< initialSize; i++)
                m_objects.Add(m_objectAllocator.Action(m_objectAllocator.Arg));
        }

        public ObjectPool(Func<T, T> objectAllocator, Action<T> objectDeallocator, int initialSize)
        {
            m_objectAllocator = new ObjectPoolAllocator<T>(objectAllocator);
            m_objectDeallocator = objectDeallocator;
            m_initialSize = initialSize;
            m_autoReleaseMemory = true;
            m_objectIndex = 0;

            m_objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
                m_objects.Add(m_objectAllocator.Action(m_objectAllocator.Arg));
        }

        public ObjectPool(ObjectPoolAllocator<T> objectAllocator, Action<T> objectDeallocator, int initialSize)
        {
            m_objectAllocator = objectAllocator;
            m_objectDeallocator = objectDeallocator;
            m_initialSize = initialSize;
            m_autoReleaseMemory = true;
            m_objectIndex = 0;

            m_objects = new List<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
                m_objects.Add(m_objectAllocator.Action(m_objectAllocator.Arg));
        }

        /// <summary>
        ///     Retrieves an object from the top of the pool
        /// </summary>
        public T Pop()
        {
            if (m_objectIndex >= m_objects.Count)
            {
                // Capacity limit has been reached, allocate new elemets
                m_objects.Add(m_objectAllocator.Action(m_objectAllocator.Arg));
                // Let Unity handle how much memory is going to be preallocated
                for (int i = m_objects.Count; i < m_objects.Capacity; i++)
                    m_objects.Add(m_objectAllocator.Action(m_objectAllocator.Arg));
            }
            
            return m_objects[m_objectIndex++];
        }

        /// <summary>
        ///     Returns an object back to pool
        /// </summary>
        public void Push(T item)
        {
            if (m_objectIndex<=0)
                throw new InvalidOperationException("Object pool is full");
            
            // If we're using less then 1/4th of memory capacity, let's free half of the allocated memory.
            // We're doing it this way so that there's a certain threshold before allocating new memory.
            // We only deallocate if there's at least m_initialSize items allocated.
            if (m_autoReleaseMemory)
            {
                int thresholdCount = m_objects.Count>>2;
                if (thresholdCount>m_initialSize && m_objectIndex<=thresholdCount)
                {
                    int halfCount = m_objects.Count>>1;

                    // Use custom deallocation if deallocator is set
                    if (m_objectDeallocator!=null)
                    {
                        for (int i = halfCount; i<m_objects.Count; i++)
                            m_objectDeallocator(m_objects[i]);
                    }

                    // Remove one half of unused items
                    m_objects.RemoveRange(halfCount, halfCount);
                }
            }

            m_objects[--m_objectIndex] = item;
        }
        
        /// <summary>
        ///    Releases all unused memory
        /// </summary>
        public void Compact()
        {
            // Use custom deallocation if deallocator is set
            if (m_objectDeallocator != null)
            {
                for (int i = m_objectIndex; i < m_objects.Count; i++)
                    m_objectDeallocator(m_objects[i]);
            }

            // Remove all unused items
            m_objects.RemoveRange(m_objectIndex, m_objects.Count-m_objectIndex);
        }
    }
}