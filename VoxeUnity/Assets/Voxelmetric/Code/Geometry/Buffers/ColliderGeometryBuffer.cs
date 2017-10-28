using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric.Code.Geometry.Buffers
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class ColliderGeometryBuffer
    {
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Vector3> Vertices = new List<Vector3>();

        /// <summary>
        ///     Clear the buffers
        /// </summary>
        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
        }

        /// <summary>
        ///     Returns true is there are no data in internal buffers
        /// </summary>
        public bool IsEmpty
        {
            get
            {
                // There will always be at least some triangles so it's safe to check just for them
                return Triangles.Count <= 0;
            }
        }

        /// <summary>
        ///     Returns true is capacity of internal buffers is non-zero
        /// </summary>
        public bool WasUsed
        {
            get
            {
                // There will always be at least some triangles so it's safe to check just for them
                return Triangles.Capacity > 0;
            }
        }

        /// <summary>
        ///     Adds triangle indices for a quad
        /// </summary>
        public void AddIndices(int offset, bool backFace)
        {
            // 0--1
            // |\ |
            // | \|
            // 3--2
            if (backFace)
            {
                Triangles.Add(offset - 4); // 0
                Triangles.Add(offset - 1); // 3
                Triangles.Add(offset - 2); // 2

                Triangles.Add(offset - 2); // 2
                Triangles.Add(offset - 3); // 1
                Triangles.Add(offset - 4); // 0
            }
            else
            {
                Triangles.Add(offset - 4); // 0
                Triangles.Add(offset - 3); // 1
                Triangles.Add(offset - 2); // 2

                Triangles.Add(offset - 2); // 2
                Triangles.Add(offset - 1); // 3
                Triangles.Add(offset - 4); // 0
            }
        }

        /// <summary>
        ///     Adds a single triangle to the buffer
        /// </summary>
        public void AddIndex(int offset)
        {
            Triangles.Add(offset);
        }

        /// <summary>
        ///     Adds vertices to the the buffer.
        /// </summary>
        public void AddVertices(Vector3[] vertices)
        {
            Vertices.AddRange(vertices);
        }

        /// <summary>
        ///     Adds a single vertex to the the buffer.
        /// </summary>
        public void AddVertex(ref Vector3 vertex)
        {
            Vertices.Add(vertex);
        }

        public void SetupMesh(Mesh mesh)
        {
            // Prepare mesh
            mesh.SetVertices(Vertices);
            mesh.SetTriangles(Triangles, 0);
            mesh.uv = null;
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;
            mesh.colors32 = null;
            mesh.tangents = null;
            mesh.normals = null;
            mesh.RecalculateNormals();
        }
    }
}