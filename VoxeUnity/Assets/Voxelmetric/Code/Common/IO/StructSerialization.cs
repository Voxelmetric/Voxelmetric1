using System;
using System.Runtime.InteropServices;
using Voxelmetric.Code.Common.Memory;

namespace Voxelmetric.Code.Common.IO
{
    public static class StructSerialization
    {
        // A helpers class to help us get rid of the Marshal.SizeOf call which can be rather slow
        internal static class TSSize<T> where T: struct
        {
            public static int ValueSize { get; private set; }

            static TSSize()
            {
                ValueSize = Marshal.SizeOf(typeof (T));
            }
        }

        // Convert a struct to byte array
        public static byte[] Serialize<T>(ref T data, MarshalMemPool pool) where T : struct
        {
            int objSize = TSSize<T>.ValueSize;
            byte[] ret = new byte[objSize];

            IntPtr buffer = pool.Pop(objSize);

            Marshal.StructureToPtr(data, buffer, true);
            Marshal.Copy(buffer, ret, 0, objSize);

            pool.Push(objSize);

            return ret;
        }

        // Convert a struct array to byte array
        public static byte[] SerializeArray<T>(T[] data, MarshalMemPool pool) where T : struct
        {
            int objSize = TSSize<T>.ValueSize;
            int objArrSize = objSize * data.Length;

            byte[] ret = new byte[objArrSize];
            IntPtr buffer = pool.Pop(objArrSize);

            long pBuffer = (long)buffer;
            for (int i = 0; i < data.Length; i++, pBuffer+=objSize)
            {
                Marshal.StructureToPtr(data[i], (IntPtr)pBuffer, true);// should be false in case struct uses pointers
                Marshal.Copy((IntPtr)pBuffer, ret, i*objSize, objSize);
            }

            pool.Push(objArrSize);

            return ret;
        }

        // Convert a byte array to a struct
        public static T Deserialize<T>(byte[] data, MarshalMemPool pool) where T : struct
        {
            //if(Marshal.SizeOf(typeof (T))<data.Length)
            //    throw new VoxeException("Input data too small");

            int objSize = data.Length;
            IntPtr buffer = pool.Pop(objSize);

            Marshal.Copy(data, 0, buffer, objSize);
            T ret = (T)Marshal.PtrToStructure(buffer, typeof(T));

            pool.Push(objSize);

            return ret;
        }

        // Convert a byte array to a struct array
        public static T[] DeserializeArray<T>(byte[] data, MarshalMemPool pool) where T : struct
        {
            //if (Marshal.SizeOf(typeof(T)) < data.Length)
            //    throw new VoxeException("Input data too small");

            int objSize = TSSize<T>.ValueSize;
            int objArrSize = data.Length / objSize;

            T[] ret = new T[objArrSize];
            IntPtr buffer = pool.Pop(data.Length);

            Marshal.Copy(data, 0, buffer, data.Length);

            long pBuffer = (long)buffer;
            for (int i = 0; i < objArrSize; i++, pBuffer += objSize)
                ret[i] = (T)Marshal.PtrToStructure((IntPtr)pBuffer, typeof(T));

            pool.Push(data.Length);

            return ret;
        }
    }
}