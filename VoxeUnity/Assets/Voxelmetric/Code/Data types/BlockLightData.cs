using System;

namespace Voxelmetric.Code.Data_types
{
    public struct BlockLightData: IEquatable<BlockLightData>
    {
        /*
         * 0- 1: sw
         * 2- 3: nw
         * 4- 5: ne
         * 6- 7: se
         */
        private readonly byte mask;
        
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

            // nw (00)
            int nw = GetVertexAO(_nSolid, _wSolid, _nwSolid);
            // ne (10)
            int ne = GetVertexAO(_nSolid, _eSolid, _neSolid);
            // sw (01)
            int sw = GetVertexAO(_sSolid, _wSolid, _swSolid);
            // se (11)
            int se = GetVertexAO(_sSolid, _eSolid, _seSolid);

            mask = (byte)(sw|(nw<<2)|(ne<<4)|(se<<6));
        }

        private static int GetVertexAO(int side1, int side2, int corner)
        {
            return (side1+side2==2) ? 3 : side1+side2+corner;
        }

        public int swAO
        {
            get { return (mask&3); }
        }

        public int nwAO
        {
            get { return (mask>>2)&3; }
        }

        public int neAO
        {
            get { return (mask>>4)&3; }
        }

        public int seAO
        {
            get { return (mask>>6)&3; }
        }

        public bool FaceRotationNecessary
        {
            get { return swAO+neAO>nwAO+seAO; }
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
