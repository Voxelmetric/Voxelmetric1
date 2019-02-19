namespace Voxelmetric.Code.Common.Memory
{
    public interface IArrayPool<T>
    {
        /// <summary>
        ///     Retrieves an array from the top of the pool
        /// </summary>
        T[] Pop();
        
        /// <summary>
        ///     Returns an array back to the pool
        /// </summary>
        void Push(T[] item);
    }
}
