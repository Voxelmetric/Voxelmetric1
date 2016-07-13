using System;
using System.Runtime.InteropServices;

namespace Voxelmetric.Code.Data_types
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public struct BlockData: IEquatable<BlockData>
    {
        private readonly ushort m_data;

        public BlockData(ushort type)
        {
            m_data = type;
        }

        public ushort Type
        {
            get { return m_data; }
        }

        public int RestoreBlockData(byte[] data, int offset)
        {
            return 0;
        }

        public byte[] ToByteArray()
        {
            return BitConverter.GetBytes(m_data);
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