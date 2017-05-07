using System;

namespace Voxelmetric.Code.Data_types
{
    public struct BlockLightData: IEquatable<BlockLightData>
    {
        /*
         * 0-nwSolid
         * 1-nSolid
         * 2-neSolid
         * 3-eSolid
         * 4-seSolid
         * 5-sSolid
         * 6-swSolid
         * 7-wSolid
         */
        private readonly int mask;

        public BlockLightData(
            int lightMask
            )
        {
            mask = lightMask;
        }

        public BlockLightData(
            bool nwSolid, bool nSolid, bool neSolid, bool eSolid,
            bool seSolid, bool sSolid, bool swSolid, bool wSolid
            )
        {
            mask = nwSolid ? 0x01 : 0;
            if (nSolid)
                mask |= 0x02;
            if (neSolid)
                mask |= 0x04;
            if (eSolid)
                mask |= 0x08;
            if (seSolid)
                mask |= 0x10;
            if (sSolid)
                mask |= 0x20;
            if (swSolid)
                mask |= 0x40;
            if (wSolid)
                mask |= 0x80;
        }

        public bool nwSolid
        {
            get { return (mask&0x01)!=0; }
        }

        public bool nSolid
        {
            get { return (mask&0x02)!=0; }
        }

        public bool neSolid
        {
            get { return (mask&0x04)!=0; }
        }

        public bool eSolid
        {
            get { return (mask&0x08)!=0; }
        }

        public bool seSolid
        {
            get { return (mask&0x10)!=0; }
        }

        public bool sSolid
        {
            get { return (mask&0x20)!=0; }
        }

        public bool swSolid
        {
            get { return (mask&0x40)!=0; }
        }

        public bool wSolid
        {
            get { return (mask&0x80)!=0; }
        }

        public override bool Equals(object obj)
        {
            if (!(obj is BlockLightData))
                return false;
            BlockLightData other = (BlockLightData)obj;
            return Equals(other);
        }

        public bool Equals(BlockLightData other)
        {
            return other.mask==mask;
        }

        public override int GetHashCode()
        {
            return mask.GetHashCode();
        }
    }
}
