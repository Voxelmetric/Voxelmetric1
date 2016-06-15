using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Rendering
{
    public class DrawCallBatcher
    {
        private const string GOPChunk = "Chunk";
        
        private readonly Chunk m_chunk;

        private readonly List<RenderBuffer> m_renderBuffers;
        private readonly List<GameObject> m_drawCalls;
        private readonly List<Renderer> m_drawCallRenderers;
        
        private bool m_visible;

        public DrawCallBatcher(Chunk chunk)
        {
            m_chunk = chunk;
            
            m_renderBuffers = new List<RenderBuffer>(1)
            {
                // Default render buffer
                new RenderBuffer()
            };
            m_drawCalls = new List<GameObject>();
            m_drawCallRenderers = new List<Renderer>();
            
            m_visible = false;
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i < m_renderBuffers.Count; i++)
                m_renderBuffers[i].Clear();

            ReleaseOldData();

            m_visible = false;
        }

        public void AddMeshData(int[] tris, VertexDataFixed[] verts, Rect texture, Vector3 offset)
        {
            RenderBuffer buffer = m_renderBuffers[m_renderBuffers.Count - 1];

            int initialVertCount = buffer.Vertices.Count;

            for (int i = 0; i<verts.Length; i++)
            {
                // If there are too many vertices we need to create a new separate buffer for them
                if (buffer.Vertices.Count+1>65000)
                {
                    buffer = new RenderBuffer();
                    m_renderBuffers.Add(buffer);
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
                    Vertex = verts[i].Vertex + offset
                };
                buffer.AddVertex(ref v);
            }
            
            for (int i = 0; i < tris.Length; i++)
                buffer.AddIndex(tris[i] + initialVertCount);
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="vertexData"> An array of 4 vertices forming the face</param>
        /// <param name="backFace">Order in which vertices are considered to be oriented. If true, this is a backface (counter clockwise)</param>
        public void AddFace(VertexDataFixed[] vertexData, bool backFace)
        {
            Assert.IsTrue(vertexData.Length>=4);

            RenderBuffer buffer = m_renderBuffers[m_renderBuffers.Count - 1];
            
            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count+4 > 65000)
            {
                buffer = new RenderBuffer();
                m_renderBuffers.Add(buffer);
            }

            // Add data to the render buffer            
            buffer.AddVertex(ref vertexData[0]);
            buffer.AddVertex(ref vertexData[1]);
            buffer.AddVertex(ref vertexData[2]);
            buffer.AddVertex(ref vertexData[3]);
            buffer.AddIndices(buffer.Vertices.Count, backFace);
        }

        /// <summary>
        ///     Finalize the draw calls
        /// </summary>
        public void Commit()
        {
            ReleaseOldData();

            // No data means there's no mesh to build
            if (m_renderBuffers[0].IsEmpty())
                return;

            for (int i = 0; i<m_renderBuffers.Count; i++)
            {
                RenderBuffer buffer = m_renderBuffers[i];
                
                var go = GameObjectProvider.PopObject(GOPChunk);
                Assert.IsTrue(go!=null);
                if (go!=null)
                {
#if DEBUG
                    go.name = m_chunk.pos.ToString();
#endif

                    Mesh mesh = Globals.MemPools.MeshPool.Pop();
                    Assert.IsTrue(mesh.vertices.Length<=0);
                    UnityMeshBuilder.BuildMesh(mesh, buffer);

                    MeshFilter filter = go.GetComponent<MeshFilter>();
                    filter.sharedMesh = null;
                    filter.sharedMesh = mesh;
                    filter.transform.position = m_chunk.world.transform.rotation*m_chunk.pos+
                                                m_chunk.world.transform.position;
                    filter.transform.rotation = m_chunk.world.transform.rotation;

                    Renderer renderer = go.GetComponent<Renderer>();
                    renderer.material = m_chunk.world.chunkMaterial;

                    m_drawCalls.Add(go);
                    m_drawCallRenderers.Add(renderer);
                }

                buffer.Clear();
            }
        }

        public void SetVisible(bool show)
        {
            for (int i = 0; i<m_drawCallRenderers.Count; i++)
            {
                Renderer renderer = m_drawCallRenderers[i];
                renderer.enabled = show;
            }
            m_visible = show && m_drawCallRenderers.Count>0;
        }

        public bool IsVisible()
        {
            return m_drawCalls.Count>0 && m_visible;
        }

        private void ReleaseOldData()
        {
            Assert.IsTrue(m_drawCalls.Count==m_drawCallRenderers.Count);
            for (int i = 0; i < m_drawCalls.Count; i++)
            {
                var go = m_drawCalls[i];
                // If the component does not exist it means nothing else has been added as well
                if (go == null)
                    continue;

                MeshFilter filter = go.GetComponent<MeshFilter>();
                filter.sharedMesh.Clear(false);
                Globals.MemPools.MeshPool.Push(filter.sharedMesh);
                filter.sharedMesh = null;

                Renderer renderer = go.GetComponent<Renderer>();
                renderer.materials[0] = null;

                GameObjectProvider.PushObject(GOPChunk, go);
            }

            m_drawCalls.Clear();
            m_drawCallRenderers.Clear();
        }
    }
}