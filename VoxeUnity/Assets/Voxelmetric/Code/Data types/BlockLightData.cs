using System;

namespace Voxelmetric.Code.Data_types
{
    public struct BlockLightData: IEquatable<BlockLightData>
    {
        /*
         * 0- 1: sw
         * 2- 3: nw
         * 4- 5: nw
         * 6- 7: se
         * 8: necessity of face rotation
         * 8-15: reserved
         */
        private readonly short mask;

        public BlockLightData(
            short lightMask
            )
        {
            mask = lightMask;
        }

        public BlockLightData(
            bool nwSolid, bool nSolid, bool neSolid, bool eSolid,
            bool seSolid, bool sSolid, bool swSolid, bool wSolid
        )
        {
            // sw
            int sw = GetVertexAO(sSolid, wSolid, swSolid);
            // nw
            int nw = GetVertexAO(nSolid, wSolid, nwSolid);
            // ne
            int ne = GetVertexAO(nSolid, eSolid, neSolid);
            // se
            int se = GetVertexAO(sSolid, eSolid, seSolid);

            mask = (short)(sw|(nw<<2)|(ne<<4)|(se<<6));

            // Rotation flag
            if (sw+ne>nw+se)
                mask |= (1<<8);
        }

        private static int GetVertexAO(bool side1, bool side2, bool corner)
        {
            if (side1 && side2)
                return 3;

            int s1 = side1 ? 1 : 0;
            int s2 = side2 ? 1 : 0;
            int c = corner ? 1 : 0;

            return s1 + s2 + c;
        }
        
        public int swAO
        {
            get { return (mask&0x03); }
        }

        public int nwAO
        {
            get { return (mask&0x0C)>>2; }
        }

        public int neAO
        {
            get { return (mask&0x30)>>4; }
        }

        public int seAO
        {
            get { return (mask&0xC0)>>6; }
        }

        public bool FaceRotationNecessary
        {
            get { return ((mask>>8)&1)!=0; }
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
