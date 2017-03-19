using System;
using System.Runtime.InteropServices;
using Voxelmetric.Code.Common.Memory;

namespace Voxelmetric.Code.Common.IO
{
    public static class StructSerialization
    {
        // A helpers class to help us get rid of the Marshal.SizeOf call which can be rather slow
        public static class TSSize<T> where T: struct
        {
            public static int ValueSize { get; private set; }

            static TSSize()
            {
                ValueSize = Marshal.SizeOf(typeof (T));
            }
        }

        // Converts a struct to a byte array
        public static byte[] Serialize<T>(MarshalMemPool pool, ref T src) where T: struct
        {
            int objSize = TSSize<T>.ValueSize;
            byte[] dst = new byte[objSize];

            IntPtr buffer = pool.Pop(objSize);
            {
                Marshal.StructureToPtr(src, buffer, true);
                Marshal.Copy(buffer, dst, 0, objSize);
            }
            pool.Push(objSize);

            return dst;
        }

        // Converts a struct to a byte array
        public static void Serialize<T>(MarshalMemPool pool, ref T src, ref byte[] dst) where T: struct
        {
            int objSize = TSSize<T>.ValueSize;

            IntPtr buffer = pool.Pop(objSize);
            {
                Marshal.StructureToPtr(src, buffer, true);
                Marshal.Copy(buffer, dst, 0, objSize);
            }
            pool.Push(objSize);
        }

        // Converts an array of structs to a byte array
        public static byte[] SerializeArray<T>(MarshalMemPool pool, T[] src, int items = -1) where T: struct
        {
            int itemsToConvert = (items<0) ? src.Length : items;
            int objSize = TSSize<T>.ValueSize;
            int objArrSize = objSize*itemsToConvert;

            byte[] dst = new byte[objArrSize];

            IntPtr pBuffer = pool.Pop(objArrSize);
            {
                long pDst = (long)pBuffer;
                for (int i = 0; i<itemsToConvert; i++, pDst += objSize)
                {
                    Marshal.StructureToPtr(src[i], (IntPtr)pDst, true);
                    Marshal.Copy((IntPtr)pDst, dst, i*objSize, objSize);
                }
            }

            return dst;
        }

        // Converts an array of structs to a byte array
        public static void SerializeArray<T>(MarshalMemPool pool, T[] src, ref byte[] dst, int items = -1)
            where T: struct
        {
            int itemsToConvert = (items<0) ? src.Length : items;
            int objSize = TSSize<T>.ValueSize;
            int objArrSize = objSize*itemsToConvert;

            IntPtr pBuffer = pool.Pop(objArrSize);
            {
                long pDst = (long)pBuffer;
                for (int i = 0; i<itemsToConvert; i++, pDst += objSize)
                {
                    Marshal.StructureToPtr(src[i], (IntPtr)pDst, true);
                    Marshal.Copy((IntPtr)pDst, dst, i*objSize, objSize);
                }
            }
            pool.Push(objArrSize);
        }

        // Converts a byte array to a struct
        public static T Deserialize<T>(MarshalMemPool pool, byte[] src) where T: struct
        {
            //if(Marshal.SizeOf(typeof (T))<data.Length)
            //    throw new VoxeException("Input data too small");

            int objSize = src.Length;
            IntPtr buffer = pool.Pop(objSize);

            Marshal.Copy(src, 0, buffer, objSize);
            T ret = (T)Marshal.PtrToStructure(buffer, typeof (T));

            pool.Push(objSize);

            return ret;
        }

        // Convert a byte array to an array of structs
        public static T[] DeserializeArray<T>(MarshalMemPool pool, byte[] src, int length = -1) where T: struct
        {
            //if (Marshal.SizeOf(typeof(T)) < data.Length)
            //    throw new VoxeException("Input data too small");

            int objSize = TSSize<T>.ValueSize;
            int len = (length<=0) ? src.Length : length;
            int objArrSize = len/objSize;

            T[] ret = new T[objArrSize];
            IntPtr buffer = pool.Pop(len);

            Marshal.Copy(src, 0, buffer, len);

            long pBuffer = (long)buffer;
            for (int i = 0; i<objArrSize; i++, pBuffer += objSize)
                ret[i] = (T)Marshal.PtrToStructure((IntPtr)pBuffer, typeof (T));

            pool.Push(len);

            return ret;
        }
    }
}