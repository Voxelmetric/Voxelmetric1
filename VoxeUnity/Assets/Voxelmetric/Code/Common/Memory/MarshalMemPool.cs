using System;
using System.Runtime.InteropServices;
using UnityEngine.Assertions;

namespace Voxelmetric.Code.Common.Memory
{
    public class MarshalMemPool
    {
        //! Allocated memory in bytes
        private readonly int m_size;
        //! Position to the beggining of the buffer
        private readonly long m_buffer;
        //! Current position in allocate array (m_buffer+x)
        private long m_pos;

        public MarshalMemPool(int initialSize)
        {
            m_size = initialSize;
            // Allocate all memory we can
            m_buffer = (long)Marshal.AllocHGlobal(initialSize);
            m_pos = m_buffer;
        }

        ~MarshalMemPool()
        {
            // Release all allocated memory in the end
            Marshal.FreeHGlobal((IntPtr)m_buffer);
        }

        public IntPtr Pop(int size)
        {
            // Do not take more than we can give!
            Assert.IsTrue(m_pos+size<m_buffer+m_size);

            m_pos += size;
            return (IntPtr)m_pos;
        }

        public void Push(int size)
        {
            // Do not return than we gave!
            Assert.IsTrue(m_pos>=m_buffer);

            m_pos -= size;
        }

        public int Left
        {
            get { return m_size - (int)(m_pos-m_buffer); }
        }

        public override string ToString()
        {
            return string.Format("{0}/{1}", (int)(m_pos-m_buffer), m_size);
        }
    }
}
