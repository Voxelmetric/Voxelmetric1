using System;
using System.Collections.Generic;
using Voxelmetric.Code.Common.Memory;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    public abstract class AObjectPool
    {
        private const int RoundSizeBy = 100;

        protected static int GetRoundedSize(int size)
        {
            int rounded = size / RoundSizeBy * RoundSizeBy;
            return rounded == size ? rounded : rounded + RoundSizeBy;
        }

        protected static T[] PopArray<T>(int size, IDictionary<int, IArrayPool<T>> pools)
        {
            int length = GetRoundedSize(size);

            IArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
            {
                pool = new ArrayPool<T>(length, 16, 1);
                pools.Add(length, pool);
            }

            return pool.Pop();
        }

        protected static void PushArray<T>(T[] array, IDictionary<int, IArrayPool<T>> pools)
        {
            int length = array.Length;

            IArrayPool<T> pool;
            if (!pools.TryGetValue(length, out pool))
                throw new InvalidOperationException("Couldn't find an array pool of length " + length);

            pool.Push(array);
        }
    }
}
