using UnityEngine;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Common.Math
{
    public static class Geometry
    {
        private enum EPlaneSide
        {
            Left,
            Right,
            Bottom,
            Top,
            Near,
            Far
        }

        private static readonly float[] RootVector = new float[4];
        private static readonly float[] ComVector = new float[4];

        /// <summary>
        ///     Calculates frustrum planes for camera.
        ///     Planes are going to be in [Left, Right, Bottom, Top, Near, Far] format
        /// </summary>
        public static void CalculateFrustumPlanes(Camera camera, ref Plane[] outPlanes)
        {
            Matrix4x4 projectionMatrix = camera.projectionMatrix;
            Matrix4x4 worldToCameraMatrix = camera.worldToCameraMatrix;
            Matrix4x4 worldToProjectionMatrix = projectionMatrix * worldToCameraMatrix;

            RootVector[0] = worldToProjectionMatrix[3, 0];
            RootVector[1] = worldToProjectionMatrix[3, 1];
            RootVector[2] = worldToProjectionMatrix[3, 2];
            RootVector[3] = worldToProjectionMatrix[3, 3];

            // Left & right plane
            ComVector[0] = worldToProjectionMatrix[0, 0];
            ComVector[1] = worldToProjectionMatrix[0, 1];
            ComVector[2] = worldToProjectionMatrix[0, 2];
            ComVector[3] = worldToProjectionMatrix[0, 3];

            CalcPlane(
                ref outPlanes[(int)EPlaneSide.Left],
                ComVector[0] + RootVector[0],
                ComVector[1] + RootVector[1],
                ComVector[2] + RootVector[2],
                ComVector[3] + RootVector[3]
                );
            CalcPlane(
                ref outPlanes[(int)EPlaneSide.Right],
                -ComVector[0] + RootVector[0],
                -ComVector[1] + RootVector[1],
                -ComVector[2] + RootVector[2],
                -ComVector[3] + RootVector[3]
                );

            // Bottom & top plane
            ComVector[0] = worldToProjectionMatrix[1, 0];
            ComVector[1] = worldToProjectionMatrix[1, 1];
            ComVector[2] = worldToProjectionMatrix[1, 2];
            ComVector[3] = worldToProjectionMatrix[1, 3];

            CalcPlane(
                ref outPlanes[(int)EPlaneSide.Bottom],
                ComVector[0] + RootVector[0],
                ComVector[1] + RootVector[1],
                ComVector[2] + RootVector[2],
                ComVector[3] + RootVector[3]
                );
            CalcPlane(
                ref outPlanes[(int)EPlaneSide.Top],
                -ComVector[0] + RootVector[0],
                -ComVector[1] + RootVector[1],
                -ComVector[2] + RootVector[2],
                -ComVector[3] + RootVector[3]
                );

            // Near & far plane
            ComVector[0] = worldToProjectionMatrix[2, 0];
            ComVector[1] = worldToProjectionMatrix[2, 1];
            ComVector[2] = worldToProjectionMatrix[2, 2];
            ComVector[3] = worldToProjectionMatrix[2, 3];

            CalcPlane(
                ref outPlanes[(int)EPlaneSide.Near],
                ComVector[0] + RootVector[0],
                ComVector[1] + RootVector[1],
                ComVector[2] + RootVector[2],
                ComVector[3] + RootVector[3]
                );
            CalcPlane(
                ref outPlanes[(int)EPlaneSide.Far],
                -ComVector[0] + RootVector[0],
                -ComVector[1] + RootVector[1],
                -ComVector[2] + RootVector[2],
                -ComVector[3] + RootVector[3]
                );

        }

        private static void CalcPlane(ref Plane inPlane, float x, float y, float z, float distance)
        {
            Vector3 normal = new Vector3(x, y, z);
            float invMagnitude = 1.0f / (float)System.Math.Sqrt(normal.x * normal.x + normal.y * normal.y + normal.z * normal.z);

            inPlane.normal = new Vector3(normal.x * invMagnitude, normal.y * invMagnitude, normal.z * invMagnitude);
            inPlane.distance = distance * invMagnitude;
        }

        /// <summary>
        ///     Does a conservative check for intersection of a AABB with frustum planes.
        ///     It is similiar to Unity3D's TestPlanesAABB, however, it accepts partial
        ///     intersections as well.
        /// </summary>
        public static bool TestPlanesAABB(Plane[] planes, Bounds aabb)
        {
            for (int i = 0; i<6; ++i)
            {
                Plane p = planes[i];
                Vector3 vPositive = new Vector3(
                    p.normal.x>0 ? aabb.max.x : aabb.min.x,
                    p.normal.y>0 ? aabb.max.y : aabb.min.y,
                    p.normal.z>0 ? aabb.max.z : aabb.min.z
                    );
                if (Vector3.Dot(vPositive, p.normal)+p.distance<0)
                    return false; // Outside the bounds
            }

            return true;
        }

        /// <summary>
        ///     Does a conservative check for intersection of a AABB with frustum planes.
        ///     It is similiar to Unity3D's TestPlanesAABB, however, it accepts partial
        ///     intersections as well.
        /// </summary>
        public static bool TestPlanesAABB(Plane[] planes, AABB aabb)
        {
            for (int i = 0; i<6; ++i)
            {
                Plane p = planes[i];
                Vector3 vPositive = new Vector3(
                    p.normal.x>0 ? aabb.maxX : aabb.minX,
                    p.normal.y>0 ? aabb.maxY : aabb.minY,
                    p.normal.z>0 ? aabb.maxZ : aabb.minZ
                    );
                if (Vector3.Dot(vPositive, p.normal)+p.distance<0)
                    return false; // Outside the bounds
            }

            return true;
        }

        /// <summary>
        ///     Does a check for intersection of a AABB with frustum planes. It returns
        ///     the number of intersecting planes. 0 means there is no intersection,
        ///     6 stands for aabb fully contained, anything else is a partial intersection
        /// </summary>
        public static int TestPlanesAABB2(Plane[] planes, Bounds aabb)
        {
            int inside = 6;
            for (int i = 0; i<6; ++i)
            {
                Plane p = planes[i];
                bool condX = p.normal.x > 0;
                bool condY = p.normal.y > 0;
                bool condZ = p.normal.z > 0;

                Vector3 vPositive = new Vector3(
                    condX ? aabb.max.x : aabb.min.x,
                    condY ? aabb.max.y : aabb.min.y,
                    condZ ? aabb.max.z : aabb.min.z
                    );
                if (Vector3.Dot(vPositive, p.normal)+p.distance<0)
                    return 0; // Outside the bounds

                Vector3 vNegative = new Vector3(
                    condX ? aabb.min.x : aabb.max.x,
                    condY ? aabb.min.y : aabb.max.y,
                    condZ ? aabb.min.z : aabb.max.z
                    );
                if (Vector3.Dot(vNegative, p.normal)+p.distance<0)
                    inside = 3; // Partial intersection
            }

            return inside; // AABB fully contained
        }

        /// <summary>
        ///     Does a check for intersection of a AABB with frustum planes. It returns
        ///     the number of intersecting planes. 0 means there is no intersection,
        ///     6 stands for aabb fully contained, anything else is a partial intersection
        /// </summary>
        public static int TestPlanesAABB2(Plane[] planes, AABB aabb)
        {
            int inside = 6;
            for (int i = 0; i<6; ++i)
            {
                Plane p = planes[i];
                bool condX = p.normal.x>0;
                bool condY = p.normal.y>0;
                bool condZ = p.normal.z>0;

                Vector3 vPositive = new Vector3(
                    condX ? aabb.maxX : aabb.minX,
                    condY ? aabb.maxY : aabb.minY,
                    condZ ? aabb.maxZ : aabb.minZ
                    );
                if (Vector3.Dot(vPositive, p.normal)+p.distance<0)
                    return 0; // Outside the bounds

                Vector3 vNegative = new Vector3(
                    condX ? aabb.minX : aabb.maxX,
                    condY ? aabb.minY : aabb.maxY,
                    condZ ? aabb.minZ : aabb.maxZ
                    );
                if (Vector3.Dot(vNegative, p.normal)+p.distance<0)
                    inside = 3; // Partial intersection
            }

            return inside; // AABB fully contained
        }
    }
}
