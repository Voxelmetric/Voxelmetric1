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
            int _nwSolid = nwSolid ? 1 : 0;
            int _nSolid = nSolid ? 1 : 0;
            int _neSolid = neSolid ? 1 : 0;
            int _eSolid = eSolid ? 1 : 0;
            int _seSolid = seSolid ? 1 : 0;
            int _sSolid = sSolid ? 1 : 0;
            int _swSolid = swSolid ? 1 : 0;
            int _wSolid = wSolid ? 1 : 0;

            // sw
            int sw = GetVertexAO(_sSolid, _wSolid, _swSolid);
            // nw
            int nw = GetVertexAO(_nSolid, _wSolid, _nwSolid);
            // ne
            int ne = GetVertexAO(_nSolid, _eSolid, _neSolid);
            // se
            int se = GetVertexAO(_sSolid, _eSolid, _seSolid);

            mask = (short)(sw|(nw<<2)|(ne<<4)|(se<<6));

            // Rotation flag
            if (sw+ne>nw+se)
                mask |= (1<<8);
        }

        private static int GetVertexAO(int side1, int side2, int corner)
        {
            return (side1+side2==2) ? 3 : side1+side2+corner;
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
