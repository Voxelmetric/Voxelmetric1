using System;
using System.Collections.Generic;

namespace Voxelmetric.Code.Common.Memory
{
    public class ArrayPoolCollection<T>
    {
        private readonly Dictionary<int, IArrayPool<T>> m_arrays;

        public ArrayPoolCollection(int size)
        {
            m_arrays = new Dictionary<int, IArrayPool<T>>(size);
        }

        public T[] PopExact(int size)
        {
            IArrayPool<T> pool;
            if (!m_arrays.TryGetValue(size, out pool))
            {
                pool = new ArrayPool<T>(size, 4, 1);
                m_arrays.Add(size, pool);
            }

            return pool.Pop();
        }

        public T[] Pop(int size)
        {
            int length = GetRoundedSize(size);
            return PopExact(length);
        }

        public void Push(T[] array)
        {
            int length = array.Length;

            IArrayPool<T> pool;
            if (!m_arrays.TryGetValue(length, out pool))
                throw new InvalidOperationException("Couldn't find an array pool of length " + length.ToString());

            pool.Push(array);
        }

        private static readonly int RoundSizeBy = 100;

        protected static int GetRoundedSize(int size)
        {
            int rounded = (size / RoundSizeBy) * RoundSizeBy;
            return rounded + RoundSizeBy;
        }

        public override string ToString()
        {
            return m_arrays.Count.ToString();
        }
    }
}
