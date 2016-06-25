using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Memory
{
    public sealed class ArrayPool<T>: IArrayPool<T>
    {
        //! Stack of arrays
        private readonly Stack<T[]> m_arrays;
        //! Length of array to allocate
        private readonly int m_arrLength;
        
        public ArrayPool(int length, int initialCapacity, int initialSize)
        {
            m_arrLength = length;

            if (initialSize>0)
            {
                // Init
                m_arrays = new Stack<T[]>(initialSize<initialCapacity ? initialCapacity : initialSize);

                for (int i = 0; i<initialSize; ++i)
                {
                    var item = Helpers.CreateArray1D<T>(length);
                    m_arrays.Push(item);
                }
            }
            else
            {
                // Init
                m_arrays = new Stack<T[]>(initialCapacity);
            }
        }

        /// <summary>
        ///     Retrieves an array from the top of the pool
        /// </summary>
        public T[] Pop()
        {
            return m_arrays.Count == 0 ? new T[m_arrLength] : m_arrays.Pop();
        }

        /// <summary>
        ///     Returns an array back to the pool
        /// </summary>
        public void Push(T[] item)
        {
            if (item==null)
                return;

            m_arrays.Push(item);
        }
    }
}