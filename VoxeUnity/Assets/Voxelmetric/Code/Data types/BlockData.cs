using System;
using System.Runtime.InteropServices;

namespace Voxelmetric.Code.Data_types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockData: IEquatable<BlockData>
    {
        public static readonly ushort TypeMask = 0x7FFF;

        /* Bits
         * 15 - solid
         * 14 - 0 - block type
        */
        private readonly ushort m_data;
        
        public BlockData(ushort data)
        {
            m_data = data;
        }

        public BlockData(ushort type, bool solid)
        {
            m_data = (ushort)(type&0x7FFF);
            if (solid)
                m_data |= 0x8000;
        }
        
        public ushort Data
        {
            get { return m_data; }
        }

        /// <summary>
        /// Fast lookup of whether the block is solid without having to take a look into block arrays
        /// </summary>
        public bool Solid
        {
            get { return (m_data>>15)!=0; }
        }

        /// <summary>
        /// Information about block's type
        /// </summary>
        public ushort Type
        {
            get { return (ushort)(m_data&TypeMask); }
        }

        public static ushort RestoreBlockData(byte[] data, int offset)
        {
            return BitConverter.ToUInt16(data, offset);
        }

        public static byte[] ToByteArray(BlockData data)
        {
            return BitConverter.GetBytes(data.m_data);
        }

        #region Object comparison

        public bool Equals(BlockData other)
        {
            return m_data==other.m_data;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is BlockData && Equals((BlockData)obj);
        }

        public override int GetHashCode()
        {
            return m_data.GetHashCode();
        }

        public static bool operator==(BlockData data1, BlockData data2)
        {
            return data1.m_data==data2.m_data;
        }

        public static bool operator!=(BlockData data1, BlockData data2)
        {
            return data1.m_data!=data2.m_data;
        }

        #endregion
    }
}