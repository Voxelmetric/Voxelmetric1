using System;
using System.Runtime.InteropServices;

namespace Voxelmetric.Code.Data_types
{
    [Serializable]
    [StructLayout(LayoutKind.Sequential)]
    public struct BlockData: IComparable<BlockData>, IEquatable<BlockData>
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

        public int CompareTo(BlockData other)
        {
            return other.m_data==m_data ? 0 : 1;
        }

        public bool Equals(BlockData other)
        {
            return other.m_data==m_data;
        }

        public override bool Equals(object obj)
        {
            return obj != null && GetHashCode() == ((BlockData)obj).GetHashCode();
        }

        public override int GetHashCode()
        {
            return m_data.GetHashCode();
        }
    }
}