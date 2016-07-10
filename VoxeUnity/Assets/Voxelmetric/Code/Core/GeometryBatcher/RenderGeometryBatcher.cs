using System.Collections.Generic;
using Assets.Voxelmetric.Code.Core.GeometryBatcher;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Rendering
{
    public class RenderGeometryBatcher: IGeometryBatcher
    {
        private const string GOPChunk = "Chunk";

        private readonly Chunk m_chunk;

        private readonly List<GeometryBuffer> m_buffers;
        private readonly List<GameObject> m_objects;
        private readonly List<Renderer> m_renderers;

        private bool m_visible;

        public RenderGeometryBatcher(Chunk chunk)
        {
            m_chunk = chunk;

            m_buffers = new List<GeometryBuffer>(1)
            {
                // Default render buffer
                new GeometryBuffer()
            };
            m_objects = new List<GameObject>();
            m_renderers = new List<Renderer>();

            m_visible = false;
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i<m_buffers.Count; i++)
                m_buffers[i].Clear();

            ReleaseOldData();

            m_visible = false;
        }

        public void AddMeshData(int[] tris, VertexDataFixed[] verts, Rect texture, Vector3 offset)
        {
            GeometryBuffer buffer = m_buffers[m_buffers.Count-1];

            int initialVertCount = buffer.Vertices.Count;

            for (int i = 0; i<verts.Length; i++)
            {
                // If there are too many vertices we need to create a new separate buffer for them
                if (buffer.Vertices.Count+1>65000)
                {
                    buffer = new GeometryBuffer();
                    m_buffers.Add(buffer);
                }

                VertexDataFixed v = new VertexDataFixed()
                {
                    Color = verts[i].Color,
                    Normal = verts[i].Normal,
                    Tangent = verts[i].Tangent,
                    // Adjust UV coordinates based on provided texture atlas
                    UV = new Vector2(
                        (verts[i].UV.x*texture.width)+texture.x,
                        (verts[i].UV.y*texture.height)+texture.y
                        ),
                    Vertex = verts[i].Vertex+offset
                };
                buffer.AddVertex(ref v);
            }

            for (int i = 0; i<tris.Length; i++)
                buffer.AddIndex(tris[i]+initialVertCount);
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="vertexData"> An array of 4 vertices forming the face</param>
        public void AddFace(VertexDataFixed[] vertexData)
        {
            Assert.IsTrue(vertexData.Length>=4);

            GeometryBuffer buffer = m_buffers[m_buffers.Count-1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count+4>65000)
            {
                buffer = new GeometryBuffer();
                m_buffers.Add(buffer);
            }

            // Add data to the render buffer
            buffer.AddVertex(ref vertexData[0]);
            buffer.AddVertex(ref vertexData[1]);
            buffer.AddVertex(ref vertexData[2]);
            buffer.AddVertex(ref vertexData[3]);
            buffer.AddIndices(buffer.Vertices.Count, false);
        }

        /// <summary>
        ///     Finalize the draw calls
        /// </summary>
        public void Commit()
        {
            ReleaseOldData();

            // No data means there's no mesh to build
            if (m_buffers[0].IsEmpty())
                return;

            for (int i = 0; i<m_buffers.Count; i++)
            {
                GeometryBuffer buffer = m_buffers[i];

                var go = GameObjectProvider.PopObject(GOPChunk);
                Assert.IsTrue(go!=null);
                if (go!=null)
                {
#if DEBUG
                    go.name = m_chunk.pos.ToString();
#endif

                    Mesh mesh = Globals.MemPools.MeshPool.Pop();
                    Assert.IsTrue(mesh.vertices.Length<=0);
                    UnityMeshBuilder.BuildGeometryMesh(mesh, buffer);

                    MeshFilter filter = go.GetComponent<MeshFilter>();
                    filter.sharedMesh = null;
                    filter.sharedMesh = mesh;
                    filter.transform.position = m_chunk.world.transform.rotation*m_chunk.pos+
                                                m_chunk.world.transform.position;
                    filter.transform.rotation = m_chunk.world.transform.rotation;

                    Renderer renderer = go.GetComponent<Renderer>();
                    renderer.material = m_chunk.world.chunkMaterial;

                    m_objects.Add(go);
                    m_renderers.Add(renderer);
                }

                buffer.Clear();
            }
        }

        public void Enable(bool show)
        {
            for (int i = 0; i<m_renderers.Count; i++)
            {
                Renderer renderer = m_renderers[i];
                renderer.enabled = show;
            }
            m_visible = show && m_renderers.Count>0;
        }

        public bool IsEnabled()
        {
            return m_objects.Count>0 && m_visible;
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

                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh.Clear(false);
                Globals.MemPools.MeshPool.Push(filter.sharedMesh);
                filter.sharedMesh = null;

                Renderer renderer = go.GetComponent<Renderer>();
                renderer.materials[0] = null;

                GameObjectProvider.PushObject(GOPChunk, go);
            }

            m_objects.Clear();
            m_renderers.Clear();
        }
    }
}