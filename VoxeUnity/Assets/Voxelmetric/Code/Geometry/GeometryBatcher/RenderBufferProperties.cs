namespace Voxelmetric.Code.Geometry.GeometryBatcher
{
    internal struct BufferProperties
    {
        private readonly int mask;

        public BufferProperties(int mask)
        {
            this.mask = mask;
        }

        public int GetMask
        {
            get { return mask; }
        }

        public static int SetColors(int mask)
        {
            return mask|1;
        }

        public static int SetTextures(int mask)
        {
            return mask|2;
        }

        public static int SetTangents(int mask)
        {
            return mask|4;
        }

        public static bool GetColors(int mask)
        {
            return (mask&1)!=0;
        }

        public static bool GetTextures(int mask)
        {
            return (mask&2)!=0;
        }

        public static bool GetTangents(int mask)
        {
            return (mask&4)!=0;
        }
    }
}