using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Rendering.GeometryBatcher
{
    public class ColliderGeometryBatcher: IGeometryBatcher<PhysicMaterial>
    {
        private readonly string m_prefabName;
        private readonly List<GeometryBuffer> m_buffers;
        private readonly List<GameObject> m_objects;
        private readonly List<Collider> m_colliders;

        private bool m_visible;

        public ColliderGeometryBatcher(string prefabName)
        {
            m_prefabName = prefabName;
            m_buffers = new List<GeometryBuffer>(1)
            {
                // Default render buffer
                new GeometryBuffer()
            };
            m_objects = new List<GameObject>();
            m_colliders = new List<Collider>();

            Clear();
        }

        /// <summary>
        ///     Clear all draw calls
        /// </summary>
        public void Clear()
        {
            for (int i = 0; i<m_buffers.Count; i++)
            {
                GeometryBuffer buffer = m_buffers[i];
                if (!buffer.IsEmpty())
                    m_buffers[i].Clear();
            }

            ReleaseOldData();

            m_visible = false;
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="vertexData"> An array of 4 vertices forming the face</param>
        public void AddFace(VertexDataFixed[] vertexData, bool backFace)
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
            buffer.AddIndices(buffer.Vertices.Count, backFace);
        }

        /// <summary>
        ///     Finalize the draw calls
        /// </summary>
        public void Commit(Vector3 position, Quaternion rotation, PhysicMaterial material
#if DEBUG
            , string debugName = null
#endif
            )
        {
            ReleaseOldData();

            // No data means there's no mesh to build
            if (m_buffers[0].IsEmpty())
                return;

            for (int i = 0; i<m_buffers.Count; i++)
            {
                GeometryBuffer buffer = m_buffers[i];

                // Create a game object for collider. Unfortunatelly, we can't use object pooling
                // here for otherwise, unity would have to rebake because of change in object position
                // and that is very time consuming.
                GameObject prefab = GameObjectProvider.GetPool(m_prefabName).Prefab;
                GameObject go = Object.Instantiate(prefab);
                go.transform.parent = GameObjectProvider.Instance.ProviderGameObject.transform;

                {
#if DEBUG
                    if (!string.IsNullOrEmpty(debugName))
                    {
                        go.name = debugName;
                        if (i>0)
                            go.name = go.name+"_"+i;
                    }
#endif

                    Mesh mesh = Globals.MemPools.MeshPool.Pop();
                    Assert.IsTrue(mesh.vertices.Length<=0);
                    UnityMeshBuilder.BuildColliderMesh(mesh, buffer);

                    MeshCollider collider = go.GetComponent<MeshCollider>();
                    collider.sharedMesh = null;
                    collider.sharedMesh = mesh;
                    collider.transform.position = position;
                    collider.transform.rotation = rotation;
                    collider.sharedMaterial = material;

                    m_objects.Add(go);
                    m_colliders.Add(collider);
                }

                buffer.Clear();
            }
        }

        public void Enable(bool show)
        {
            for (int i = 0; i<m_colliders.Count; i++)
            {
                Collider collider = m_colliders[i];
                collider.enabled = show;
            }
            m_visible = show && m_colliders.Count>0;
        }

        public bool IsEnabled()
        {
            return m_objects.Count>0 && m_visible;
        }

        private void ReleaseOldData()
        {
            Assert.IsTrue(m_objects.Count==m_colliders.Count);
            for (int i = 0; i<m_objects.Count; i++)
            {
                var go = m_objects[i];
                // If the component does not exist it means nothing else has been added as well
                if (go==null)
                    continue;

#if DEBUG
                go.name = m_prefabName;
#endif

                MeshCollider collider = go.GetComponent<MeshCollider>();
                collider.sharedMesh.Clear(false);
                Globals.MemPools.MeshPool.Push(collider.sharedMesh);
                collider.sharedMesh = null;
                collider.sharedMaterial = null;

                Object.DestroyImmediate(go);
            }

            if(m_objects.Count>0)
                m_objects.Clear();
            if(m_colliders.Count>0)
                m_colliders.Clear();
        }
    }
}