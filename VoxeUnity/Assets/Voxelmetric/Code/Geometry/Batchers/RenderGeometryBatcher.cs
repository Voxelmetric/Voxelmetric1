using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Geometry.Buffers;

namespace Voxelmetric.Code.Geometry.Batchers
{
    public class RenderGeometryBatcher: IGeometryBatcher
    {
        private readonly string m_prefabName;
        //! Materials our meshes are to use
        private readonly Material[] m_materials;
        //! A list of buffers for each material
        private readonly List<RenderGeometryBuffer> [] m_buffers;
        //! GameObjects used to hold our geometry
        private readonly List<GameObject> m_objects;
        //! A list of renderer used to render our geometry
        private readonly List<Renderer> m_renderers;

        private bool m_enabled;
        public bool Enabled
        {
            set
            {
                for (int i = 0; i < m_renderers.Count; i++)
                {
                    Renderer renderer = m_renderers[i];
                    renderer.enabled = value;
                }
                m_enabled = value;
            }
            get
            {
                return m_enabled;
            }
        }

        public RenderGeometryBatcher(string prefabName, Material[] materials)
        {
            m_prefabName = prefabName;
            m_materials = materials;

            int buffersCount = materials == null || materials.Length < 1 ? 1 : materials.Length;
            m_buffers = new List<RenderGeometryBuffer>[buffersCount];

            for (int i = 0; i<m_buffers.Length; i++)
            {
                /* TODO: Let's be optimistic and allocate enough room for just one buffer. It's going to suffice
                 * in >99% of cases. However, this prediction should maybe be based on chunk size rather then
                 * optimism. The bigger the chunk the more likely we're going to need to create more meshes to
                 * hold its geometry because of Unity's 65k-vertices limit per mesh. For chunks up to 32^3 big
                 * this should not be an issue, though.
                 */
                m_buffers[i] = new List<RenderGeometryBuffer>(1)
                {
                    // Default render buffer
                    new RenderGeometryBuffer()
                };
            }

            m_objects = new List<GameObject>(1);
            m_renderers = new List<Renderer>(1);

            Clear();
        }

        public void Reset()
        {
            // Buffers need to be reallocated. Otherwise, more and more memory would be consumed by them. This is
            // because internal arrays grow in capacity and we can't simply release their memory by calling Clear().
            // Objects and renderers are fine, because there's usually only 1 of them. In some extreme cases they
            // may grow more but only by 1 or 2 (and only if Env.ChunkPow>5).
            for (int i = 0; i<m_buffers.Length; i++)
            {
                var geometryBuffer = m_buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    if (geometryBuffer[j].WasUsed)
                        geometryBuffer[j] = new RenderGeometryBuffer();
                }
            }

            ReleaseOldData();
            m_enabled = false;
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i<m_buffers.Length; i++)
            {
                var geometryBuffer = m_buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    geometryBuffer[j].Clear();
                }
            }

            ReleaseOldData();
            m_enabled = false;
        }

        private static void PrepareColors(ref List<Color32> colors, List<Vector3> vertices, int initialVertexCount)
        {
            if (colors == null)
                colors = new List<Color32>(vertices.Capacity);
            else if (colors.Count < initialVertexCount)
            {
                // Fill in colors if necessary
                colors.Capacity = vertices.Capacity;
                int diff = initialVertexCount - colors.Count;
                for (int i = 0; i < diff; i++)
                    colors.Add(new Color32());
            }
        }

        private static void PrepareUVs(ref List<Vector2> uvs, List<Vector3> vertices, int initialVertexCount)
        {
            if (uvs == null)
                uvs = new List<Vector2>(vertices.Capacity);
            else if (uvs.Count < initialVertexCount)
            {
                // Fill in colors if necessary
                uvs.Capacity = vertices.Capacity;
                int diff = initialVertexCount - uvs.Count;
                for (int i = 0; i < diff; i++)
                    uvs.Add(Vector2.zero);
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="tris">Triangles to be processed</param>
        /// <param name="verts">Vertices to be processed</param>
        /// <param name="colors">Colors to be processed</param>
        /// <param name="offset">Offset to apply to verts</param>
        public void AddMeshData(int materialID, int[] tris, Vector3[] verts, Color32[] colors, Vector3 offset)
        {
            // Each face consists of 6 triangles and 4 faces
            Assert.IsTrue(((verts.Length * 3) >> 1) == tris.Length);
            Assert.IsTrue((verts.Length & 3) == 0);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            int startOffset = 0;
            int leftToProcess = verts.Length;
            while (leftToProcess > 0)
            {
                int left = Math.Min(leftToProcess, 65000);

                int leftInBuffer = 65000 - buffer.Vertices.Count;
                if (leftInBuffer <= 0)
                {
                    buffer = new RenderGeometryBuffer
                    {
                        Colors = new List<Color32>()
                    };

                    buffer.Triangles.Capacity = left;
                    buffer.Vertices.Capacity = left;
                    buffer.Colors.Capacity = left;

                    holder.Add(buffer);
                }
                else
                    left = Math.Min(left, leftInBuffer);

                int max = startOffset+left;
                int maxTris = (max * 3)>>1;
                int offsetTri = (startOffset * 3)>>1;
                
                // Add vertices
                int initialVertexCount = buffer.Vertices.Count;
                for (int i = startOffset; i<max; i++)
                    buffer.Vertices.Add(verts[i]+offset);

                // Add colors
                PrepareColors(ref buffer.Colors, buffer.Vertices, initialVertexCount);
                for (int i = startOffset; i < max; i++)
                    buffer.Colors.Add(colors[i]);

                // Add triangles
                for (int i = offsetTri; i<maxTris; i++)
                    buffer.Triangles.Add(tris[i]+initialVertexCount);

                leftToProcess -= left;
                startOffset += left;
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="tris">Triangles to be processed</param>
        /// <param name="verts">Vertices to be processed</param>
        /// <param name="uvs">UVs to be processed</param>
        /// <param name="texture">Texture coordinates</param>
        /// <param name="offset">Offset to apply to vertices</param>
        public void AddMeshData(int materialID, int[]tris, Vector3[] verts, Vector2[] uvs, ref Rect texture, Vector3 offset)
        {
            // Each face consists of 6 triangles and 4 faces
            Assert.IsTrue(((verts.Length * 3) >> 1) == tris.Length);
            Assert.IsTrue((verts.Length & 3) == 0);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            int startOffset = 0;
            int leftToProcess = verts.Length;
            while (leftToProcess>0)
            {
                int left = Math.Min(leftToProcess, 65000);

                int leftInBuffer = 65000-buffer.Vertices.Count;
                if (leftInBuffer<=0)
                {
                    buffer = new RenderGeometryBuffer
                    {
                        UV1s = new List<Vector2>()
                    };

                    buffer.Triangles.Capacity = left;
                    buffer.Vertices.Capacity = left;
                    buffer.UV1s.Capacity = left;

                    holder.Add(buffer);
                }
                else
                    left = Math.Min(left, leftInBuffer);
                
                int max = startOffset + left;
                int maxTris = (max * 3)>>1;
                int offsetTri = (startOffset * 3)>>1;

                // Add vertices
                int initialVertexCount = buffer.Vertices.Count;
                for (int i = startOffset; i<max; i++)
                    buffer.Vertices.Add(verts[i]+offset);

                // Add UVs
                PrepareUVs(ref buffer.UV1s, buffer.Vertices, initialVertexCount);
                for (int i = startOffset; i<max; i++)
                    // Adjust UV coordinates according to provided texture atlas
                    buffer.UV1s.Add(new Vector2(
                                        (uvs[i].x * texture.width)+texture.x,
                                        (uvs[i].y * texture.height)+texture.y
                                    ));

                // Add triangles
                for (int i = offsetTri; i<maxTris; i++)
                    buffer.Triangles.Add(tris[i]+initialVertexCount);

                leftToProcess -= left;
                startOffset += left;
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="tris">Triangles to be processed</param>
        /// <param name="verts">Vertices to be processed</param>
        /// <param name="colors">Colors to be processed</param>
        /// <param name="uvs">UVs to be processed</param>
        /// <param name="texture">Texture coordinates</param>
        /// <param name="offset">Offset to apply to vertices</param>
        public void AddMeshData(int materialID, int[] tris, Vector3[] verts, Color32[] colors, Vector2[] uvs, ref Rect texture, Vector3 offset)
        {
            // Each face consists of 6 triangles and 4 faces
            Assert.IsTrue(((verts.Length * 3) >> 1) == tris.Length);
            Assert.IsTrue((verts.Length & 3) == 0);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            int startOffset = 0;
            int leftToProcess = verts.Length;
            while (leftToProcess > 0)
            {
                int left = Math.Min(leftToProcess, 65000);

                int leftInBuffer = 65000 - buffer.Vertices.Count;
                if (leftInBuffer <= 0)
                {
                    buffer = new RenderGeometryBuffer
                    {
                        UV1s = new List<Vector2>(),
                        Colors = new List<Color32>()
                    };

                    buffer.Triangles.Capacity = left;
                    buffer.Vertices.Capacity = left;
                    buffer.UV1s.Capacity = left;
                    buffer.Colors.Capacity = left;

                    holder.Add(buffer);
                }
                else
                    left = Math.Min(left, leftInBuffer);

                int max = startOffset+left;
                int maxTris = (max * 3)>>1;
                int offsetTri = (startOffset * 3)>>1;

                // Add vertices
                int initialVertexCount = buffer.Vertices.Count;
                for (int i = startOffset; i<max; i++)
                    buffer.Vertices.Add(verts[i]+offset);

                // Add UVs
                PrepareUVs(ref buffer.UV1s, buffer.Vertices, initialVertexCount);
                for (int i = startOffset; i<max; i++)
                    // Adjust UV coordinates according to provided texture atlas
                    buffer.UV1s.Add(new Vector2(
                                        (uvs[i].x * texture.width)+texture.x,
                                        (uvs[i].y * texture.height)+texture.y
                                    ));

                // Add colors
                PrepareColors(ref buffer.Colors, buffer.Vertices, initialVertexCount);
                for (int i = startOffset; i < max; i++)
                    buffer.Colors.Add(colors[i]);

                // Add triangles
                for (int i = offsetTri; i<maxTris; i++)
                    buffer.Triangles.Add(tris[i]+initialVertexCount);

                leftToProcess -= left;
                startOffset += left;
            }
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="uvs">An array of 4 UV coordinates</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, Vector2[] uvs, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count + 4 > 65000)
            {
                buffer = new RenderGeometryBuffer
                {
                    UV1s = new List<Vector2>()
                };
                holder.Add(buffer);
            }

            // Add vertices
            int initialVertexCount = buffer.Vertices.Count;
            buffer.Vertices.AddRange(verts);

            // Add indices
            buffer.AddIndices(buffer.Vertices.Count, backFace);

            // Add UVs
            PrepareUVs(ref buffer.UV1s, buffer.Vertices, initialVertexCount);
            buffer.UV1s.AddRange(uvs);
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="colors">An array of 4 colors</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, Color32[] colors, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count + 4 > 65000)
            {
                buffer = new RenderGeometryBuffer
                {
                    Colors = new List<Color32>()
                };
                holder.Add(buffer);
            }

            // Add vertices
            int initialVertexCount = buffer.Vertices.Count;
            buffer.Vertices.AddRange(verts);

            // Add colors
            PrepareColors(ref buffer.Colors, buffer.Vertices, initialVertexCount);
            buffer.Colors.AddRange(colors);

            // Add indices
            buffer.AddIndices(buffer.Vertices.Count, backFace);
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="materialID">ID of material to use when building the mesh</param>
        /// <param name="verts"> An array of 4 vertices forming the face</param>
        /// <param name="colors">An array of 4 colors</param>
        /// <param name="uvs">An array of 4 UV coordinates</param>
        /// <param name="backFace">If false, vertices are added clock-wise</param>
        public void AddFace(int materialID, Vector3[] verts, Color32[] colors, Vector2[] uvs, bool backFace)
        {
            Assert.IsTrue(verts.Length == 4);

            var holder = m_buffers[materialID];
            var buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count + 4 > 65000)
            {
                buffer = new RenderGeometryBuffer
                {
                    UV1s = new List<Vector2>(),
                    Colors = new List<Color32>()
                };
                holder.Add(buffer);
            }

            // Add vertices
            int initialVertexCount = buffer.Vertices.Count;
            buffer.Vertices.AddRange(verts);

            // Add UVs
            PrepareUVs(ref buffer.UV1s, buffer.Vertices, initialVertexCount);
            buffer.UV1s.AddRange(uvs);

            // Add colors
            PrepareColors(ref buffer.Colors, buffer.Vertices, initialVertexCount);
            buffer.Colors.AddRange(colors);

            // Add indices
            buffer.AddIndices(buffer.Vertices.Count, backFace);
        }

        public void Commit(Vector3 position, Quaternion rotation
#if DEBUG
            , string debugName = null
#endif
        )
        {
            ReleaseOldData();

            for (int j = 0; j<m_buffers.Length; j++)
            {
                var holder = m_buffers[j];

                for (int i = 0; i<holder.Count; i++)
                {
                    var buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                        continue;

                    var go = GameObjectProvider.PopObject(m_prefabName);
                    Assert.IsTrue(go!=null);
                    if (go!=null)
                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.MeshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length<=0);
                        buffer.SetupMesh(mesh, true);

                        MeshFilter filter = go.GetComponent<MeshFilter>();
                        filter.sharedMesh = null;
                        filter.sharedMesh = mesh;
                        filter.transform.position = position;
                        filter.transform.rotation = rotation;

                        Renderer renderer = go.GetComponent<Renderer>();
                        renderer.sharedMaterial = (m_materials==null || m_materials.Length<1) ? null : m_materials[j];

                        m_objects.Add(go);
                        m_renderers.Add(renderer);
                    }

                    buffer.Clear();
                }
            }
        }

        /// <summary>
        ///     Finalize the draw calls
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation, ref Bounds bounds
#if DEBUG
            , string debugName = null
#endif
        )
        {
            ReleaseOldData();

            for (int j = 0; j<m_buffers.Length; j++)
            {
                var holder = m_buffers[j];

                for (int i = 0; i<holder.Count; i++)
                {
                    var buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty)
                        continue;

                    var go = GameObjectProvider.PopObject(m_prefabName);
                    Assert.IsTrue(go!=null);
                    if (go!=null)
                    {
#if DEBUG
                        go.name = string.Format(debugName, "_", i.ToString());
#endif

                        Mesh mesh = Globals.MemPools.MeshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length<=0);
                        buffer.SetupMesh(mesh, false);
                        mesh.bounds = bounds;

                        MeshFilter filter = go.GetComponent<MeshFilter>();
                        filter.sharedMesh = null;
                        filter.sharedMesh = mesh;
                        filter.transform.position = position;
                        filter.transform.rotation = rotation;

                        Renderer renderer = go.GetComponent<Renderer>();
                        renderer.sharedMaterial = (m_materials==null || m_materials.Length<1) ? null : m_materials[j];

                        m_objects.Add(go);
                        m_renderers.Add(renderer);
                    }

                    buffer.Clear();
                }
            }
        }

        private void ReleaseOldData()
        {
            Assert.IsTrue(m_objects.Count==m_renderers.Count);
            for (int i = 0; i<m_objects.Count; i++)
            {
                var go = m_objects[i];
                // If the component does not exist it means nothing else has been added as well
                if (go==null)
                    continue;

#if DEBUG
                go.name = m_prefabName;
#endif

                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh.Clear(false);
                Globals.MemPools.MeshPool.Push(filter.sharedMesh);
                filter.sharedMesh = null;

                Renderer renderer = go.GetComponent<Renderer>();
                renderer.sharedMaterial = null;

                GameObjectProvider.PushObject(m_prefabName, go);
            }

            m_objects.Clear();
            m_renderers.Clear();
        }
    }
}