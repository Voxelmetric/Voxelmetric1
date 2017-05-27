using System.Collections.Generic;
using UnityEngine;

namespace Voxelmetric.Code.Geometry
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class ColliderGeometryBuffer
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<int> Triangles = new List<int>();

        /// <summary>
        ///     Clear the render buffer
        /// </summary>
        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
    }

        public bool IsEmpty()
        {
            return Vertices.Count <= 0;
        }

        public bool WasUsed()
        {
            return Vertices.Capacity > 0;
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

        public void AddIndex(int offset)
        {
            Triangles.Add(offset);
        }

        /// <summary>
        ///     Adds the vertices to the render buffer.
        /// </summary>
        public void AddVertices(Vector3[] vertices)
        {
            Vertices.AddRange(vertices);
        }

        public void AddVertex(ref Vector3 vertex)
        {
            Vertices.Add(vertex);
        }
    }
}