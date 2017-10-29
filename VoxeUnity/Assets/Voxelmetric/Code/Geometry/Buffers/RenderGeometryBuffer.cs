using System.Collections.Generic;
using NUnit.Framework;
using UnityEngine;

namespace Voxelmetric.Code.Geometry.Buffers
{
    public class RenderGeometryBuffer
    {
        public readonly List<int> Triangles = new List<int>();
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public List<Vector2> UV1s;
        public List<Color32> Colors;
        public List<Vector4> Tangents;
        
        /// <summary>
        ///     Clear the buffers
        /// </summary>
        public void Clear()
        {
            Vertices.Clear();
            Triangles.Clear();
            if (UV1s!=null)
                UV1s.Clear();
            if (Colors!=null)
                Colors.Clear();
            if (Tangents!=null)
                Tangents.Clear();
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

        public bool HasUV1
        {
            get { return UV1s!=null; }
        }

        public bool HasColors
        {
            get { return UV1s != null; }
        }

        public bool HasTangents
        {
            get { return Tangents != null; }
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

        public void SetupMesh(Mesh mesh)
        {
            // Vertices & indices
            mesh.SetVertices(Vertices);
            mesh.SetTriangles(Triangles, 0);

            // UVs
            mesh.uv = null;
            if (UV1s!=null)
            {
                Assert.IsTrue(UV1s.Count<=Vertices.Count);
                if (UV1s.Count<Vertices.Count)
                {
                    // Fill in UVs if necessary
                    if (UV1s.Capacity<Vertices.Count)
                        UV1s.Capacity = Vertices.Count;
                    int diff = Vertices.Count-UV1s.Count;
                    for (int i = 0; i<diff; i++)
                        UV1s.Add(Vector2.zero);
                }
                mesh.SetUVs(0, UV1s);
            }
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;

            // Colors
            mesh.colors = null;
            if (Colors!=null)
            {
                Assert.IsTrue(Colors.Count <= Vertices.Count);
                if (Colors.Count<Vertices.Count)
                {
                    // Fill in colors if necessary
                    if (Colors.Capacity<Vertices.Count)
                        Colors.Capacity = Vertices.Count;
                    int diff = Vertices.Count-Colors.Count;
                    for (int i = 0; i<diff; i++)
                        Colors.Add(new Color32(255, 255, 255, 255));
                }
                mesh.SetColors(Colors);
            }
            else
            {
                // TODO: Use white color if no color data is supplied?
            }

            // Tangents
            mesh.tangents = null;
            if (Tangents!=null)
            {
                Assert.IsTrue(Tangents.Count <= Vertices.Count);
                if (Tangents.Count<Vertices.Count)
                {
                    // Fill in tangents if necessary
                    if (Tangents.Capacity<Vertices.Count)
                        Tangents.Capacity = Vertices.Count;
                    int diff = Vertices.Count-Tangents.Count;
                    for (int i = 0; i<diff; i++)
                        Tangents.Add(Vector4.zero);
                }
                mesh.SetTangents(Tangents);
            }

            // Normals
            mesh.normals = null;
            mesh.RecalculateNormals();
        }
    }
}