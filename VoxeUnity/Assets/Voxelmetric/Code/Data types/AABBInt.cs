using System;

namespace Voxelmetric.Code.Data_types
{
    public struct AABBInt: IEquatable<AABBInt>
    {
        public readonly int minX;
        public readonly int minY;
        public readonly int minZ;
        public readonly int maxX;
        public readonly int maxY;
        public readonly int maxZ;

        public AABBInt(int x1, int y1, int z1, int x2, int y2, int z2)
        {
            minX = x1;
            minY = y1;
            minZ = z1;
            maxX = x2;
            maxY = y2;
            maxZ = z2;
        }

        public bool IsInside(ref Vector3Int pos)
        {
            return pos.x<maxX && pos.y<maxY && pos.z<maxZ &&
                   pos.x>=minX && pos.y>=minY && pos.z>=minZ;
        }

        public bool IsInside(ref AABBInt aab2)
        {
            return aab2.maxX<maxX && aab2.maxY<maxY && aab2.maxZ<maxZ &&
                   aab2.minX>=minX && aab2.minY>=minY && aab2.minZ>=minZ;
        }

        #region Struct comparison

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = minX;
                hashCode = (hashCode*397)^minY;
                hashCode = (hashCode*397)^minZ;
                hashCode = (hashCode*397)^maxX;
                hashCode = (hashCode*397)^maxY;
                hashCode = (hashCode*397)^maxZ;
                return hashCode;
            }
        }

        public override bool Equals(object obj)
        {
            if (ReferenceEquals(null, obj))
                return false;
            return obj is AABBInt && Equals((AABBInt)obj);
        }

        public bool Equals(AABBInt other)
        {
            return minX==other.minX && minY==other.minY && minZ==other.minZ &&
                   maxX==other.maxX && maxY==other.maxY && maxZ==other.maxZ;
        }

        public static bool operator==(AABBInt a, AABBInt b)
        {
            return a.minX==b.minX && a.minY==b.minY && a.minZ==b.minZ &&
                   a.maxX==b.maxX && a.maxY==b.maxY && a.maxZ==b.maxZ;
        }

        public static bool operator!=(AABBInt a, AABBInt b)
        {
            return a.minX!=b.minX || a.minY!=b.minY || a.minZ!=b.minZ ||
                   a.maxX!=b.maxX || a.maxY!=b.maxY || a.maxZ!=b.maxZ;
        }

        #endregion
    }
}
