using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Rendering
{
    /// <summary>
    ///     A simple intermediate container for mesh data
    /// </summary>
    public class GeometryBuffer
    {
        public readonly List<VertexDataFixed> Vertices = new List<VertexDataFixed>();
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
            return (Vertices.Count <= 0);
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
                Triangles.Add(offset - 4); // 2
                Triangles.Add(offset - 2); // 0
                Triangles.Add(offset - 3); // 1

                Triangles.Add(offset - 4); // 3
                Triangles.Add(offset - 1); // 0
                Triangles.Add(offset - 2); // 2
            }
            else
            {
                Triangles.Add(offset - 4); // 2
                Triangles.Add(offset - 3); // 1
                Triangles.Add(offset - 2); // 0

                Triangles.Add(offset - 4); // 3
                Triangles.Add(offset - 2); // 2
                Triangles.Add(offset - 1); // 0
            }
        }

        public void AddIndex(int offset)
        {
            Triangles.Add(offset);
        }

        /// <summary>
        ///     Adds the vertices to the render buffer.
        /// </summary>
        public void AddVertices(VertexDataFixed[] vertices)
        {
            Vertices.AddRange(vertices);
        }

        public void AddVertex(ref VertexDataFixed vertex)
        {
            Vertices.Add(vertex);
        }

        public void GenerateTangents(LocalPools pools)
        {
            var tan1 = pools.PopVector3Array(Vertices.Count);
            var tan2 = pools.PopVector3Array(Vertices.Count);

            for (int t = 0; t < Triangles.Count; t += 3)
            {
                int i1 = Triangles[t + 0];
                int i2 = Triangles[t + 1];
                int i3 = Triangles[t + 2];

                VertexDataFixed vd1 = Vertices[i1];
                VertexDataFixed vd2 = Vertices[i2];
                VertexDataFixed vd3 = Vertices[i3];

                Vector3 v1 = vd1.Vertex;
                Vector3 v2 = vd2.Vertex;
                Vector3 v3 = vd3.Vertex;

                Vector2 w1 = vd1.UV;
                Vector2 w2 = vd2.UV;
                Vector2 w3 = vd3.UV;

                float x1 = v2.x - v1.x;
                float y1 = v2.y - v1.y;
                float z1 = v2.z - v1.z;

                float x2 = v3.x - v1.x;
                float y2 = v3.y - v1.y;
                float z2 = v3.z - v1.z;

                float s1 = w2.x - w1.x;
                float s2 = w3.x - w1.x;

                float t1 = w2.y - w1.y;
                float t2 = w3.y - w1.y;

                // Avoid division by zero
                float div = s1 * t2 - s2 * t1;
                float r = (Mathf.Abs(div) > Mathf.Epsilon) ? (1f / div) : 0f;

                Vector3 sdir = new Vector3((t2 * x1 - t1 * x2) * r, (t2 * y1 - t1 * y2) * r, (t2 * z1 - t1 * z2) * r);
                Vector3 tdir = new Vector3((s1 * x2 - s2 * x1) * r, (s1 * y2 - s2 * y1) * r, (s1 * z2 - s2 * z1) * r);

                tan1[i1] += sdir;
                tan1[i2] += sdir;
                tan1[i3] += sdir;

                tan2[i1] += tdir;
                tan2[i2] += tdir;
                tan2[i3] += tdir;
            }

            for (int v = 0; v < Vertices.Count; ++v)
            {
                VertexDataFixed vd = Vertices[v];

                Vector3 n = vd.Normal;
                Vector3 t = tan1[v];

                //Vector3 tmp = (t - n*Vector3.Dot(n, t)).normalized;
                //tangents[v] = new Vector4(tmp.x, tmp.y, tmp.z);
                Vector3.OrthoNormalize(ref n, ref t);

                vd.Tangent = new Vector4(
                    t.x, t.y, t.z,
                    (Vector3.Dot(Vector3.Cross(n, t), tan2[v]) < 0.0f) ? -1.0f : 1.0f
                    );

                tan1[v] = Vector3.zero;
                tan2[v] = Vector3.zero;
            }

            pools.PushVector3Array(tan1);
            pools.PushVector3Array(tan2);
        }
    }
}