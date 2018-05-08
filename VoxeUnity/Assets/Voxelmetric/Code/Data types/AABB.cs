using UnityEngine;

namespace Voxelmetric.Code.Data_types
{
    public struct AABB
    {
        public readonly float minX;
        public readonly float minY;
        public readonly float minZ;
        public readonly float maxX;
        public readonly float maxY;
        public readonly float maxZ;

        public AABB(float x1, float y1, float z1, float x2, float y2, float z2)
        {
            minX = x1;
            minY = y1;
            minZ = z1;
            maxX = x2;
            maxY = y2;
            maxZ = z2;
        }

        public bool IsInside(float x, float y, float z)
        {
            return x>minX && x<maxX &&
                   y>minY && y<maxY &&
                   z>minY && z<maxZ;
        }

        public bool IsInside(ref Vector3 pos)
        {
            return pos.x>minX && pos.x<maxX &&
                   pos.y>minY && pos.y<maxY &&
                   pos.z>minZ && pos.z<maxZ;
        }
    }
}
