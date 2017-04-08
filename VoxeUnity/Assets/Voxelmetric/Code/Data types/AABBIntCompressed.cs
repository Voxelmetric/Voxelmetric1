using System;

namespace Voxelmetric.Code.Data_types
{
    /*
     * A compressed representation of AABB with integer coordinates.
     * For each coordinates there are 10 bits reserved. Therefore, each axis can contain values from 0..1023
     * 
     * x=0..9, y=10..20, z=21..31
     */

    public struct AABBIntCompressed: IEquatable<AABBIntCompressed>
    {
        public readonly int min;
        public readonly int max;

        public int minX
        {
            get { return min&0x3FF; }
        }

        public int minY
        {
            get { return (min>>10)&0x3FF; }
        }

        public int minZ
        {
            get { return min>>21; }
        }

        public int maxX
        {
            get { return max&0x3FF; }
        }

        public int maxY
        {
            get { return (max>>10)&0x3FF; }
        }

        public int maxZ
        {
            get { return max>>21; }
        }

        public AABBIntCompressed(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            min = (x1&0x3FF)|((y1&0x3FF)<<10)|(z1<<21);
            max = (x2&0x3FF)|((y2&0x3FF)<<10)|(z2<<21);
        }

        public bool IsInside(int x, int y, int z)
        {
            return x>(min&0x3FF) && x<(max&0x3FF) &&
                   z>(min>>21) && z<(max>>21) &&
                   y>((min>>10)&0x3FF) && y<((max>>10)&0x3FF);

        }

        public bool IsInside(Vector3Int pos)
        {
            return pos.x>(min&0x3FF) && pos.x<(max&0x3FF) &&
                   pos.z>(min>>21) && pos.z<(max>>21) &&
                   pos.y>((min>>10)&0x3FF) && pos.y<((max>>10)&0x3FF);
        }

        #region Object comparison

        public bool Equals(AABBIntCompressed other)
        {
            return min==other.min && max==other.max;
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is AABBIntCompressed && Equals((AABBIntCompressed)obj);
        }

        public override int GetHashCode()
        {
            unchecked
            {
                return (min.GetHashCode()*397)^max.GetHashCode();
            }
        }

        public static bool operator==(AABBIntCompressed data1, AABBIntCompressed data2)
        {
            return data1.min==data2.min && data1.max==data2.max;
        }

        public static bool operator!=(AABBIntCompressed data1, AABBIntCompressed data2)
        {
            return data1.min!=data2.min || data1.max!=data2.max;
        }

        #endregion
    }
}
