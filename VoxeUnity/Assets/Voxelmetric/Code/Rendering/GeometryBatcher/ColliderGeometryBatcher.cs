using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Builders;
using Voxelmetric.Code.Common.MemoryPooling;

namespace Voxelmetric.Code.Rendering.GeometryBatcher
{
    public class ColliderGeometryBatcher: IGeometryBatcher
    {
        private readonly string m_prefabName;
        //! Materials our meshes are to use
        private readonly PhysicMaterial[] m_materials;
        //! A list of buffers for each material
        private readonly List<GeometryBuffer>[] m_buffers;
        //! GameObjects used to hold our geometry
        private readonly List<GameObject> m_objects;
        //! A list of renderer used to render our geometry
        private readonly List<Collider> m_colliders;

        private bool m_enabled;

        public bool Enabled
        {
            set
            {
                for (int i = 0; i < m_colliders.Count; i++)
                {
                    Collider collider = m_colliders[i];
                    collider.enabled = value;
                }
                m_enabled = value && m_colliders.Count > 0;
            }
            get
            {
                return m_enabled;
            }
        }

        public ColliderGeometryBatcher(string prefabName, PhysicMaterial[] materials)
        {
            m_prefabName = prefabName;
            m_materials = materials;

            int buffersLen = (materials==null || materials.Length<1) ? 1 : materials.Length;
            m_buffers = new List<GeometryBuffer>[buffersLen];
            for (int i = 0; i < m_buffers.Length; i++)
            {
                /* TODO: Let's be optimistic and allocate enough room for just one buffer. It's going to suffice
                 * in >99% of cases. However, this prediction should maybe be based on chunk size rather then
                 * optimism. The bigger the chunk the more likely we're going to need to create more meshes to
                 * hold its geometry because of Unity's 65k-vertices limit per mesh. For chunks up to 32^3 big
                 * this should not be an issue, though.
                 */
                m_buffers[i] = new List<GeometryBuffer>(1)
                {
                    // Default render buffer
                    new GeometryBuffer()
                };
            }

            m_objects = new List<GameObject>();
            m_colliders = new List<Collider>();

            Clear();
        }

        public void Reset()
        {
            // Buffers need to be reallocated. Otherwise, more and more memory would be consumed by them. This is
            // because internal arrays grow in capacity and we can't simply release their memory by calling Clear().
            // Objects and renderers are fine, because there's usually only 1 of them. In some extreme cases they
            // may grow more but only by 1 or 2 (and only if Env.ChunkPow>5).
            for (int i = 0; i < m_buffers.Length; i++)
            {
                var geometryBuffer = m_buffers[i];
                for (int j = 0; j < geometryBuffer.Count; j++)
                {
                    if (geometryBuffer[j].WasUsed())
                        geometryBuffer[j] = new GeometryBuffer();
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
            foreach (var holder in m_buffers)
            {
                for (int i = 0; i < holder.Count; i++)
                    holder[i].Clear();
            }

            ReleaseOldData();
            m_enabled = false;
        }

        /// <summary>
        ///     Addds one face to our render buffer
        /// </summary>
        /// <param name="vertexData"> An array of 4 vertices forming the face</param>
        public void AddFace(VertexData[] vertexData, bool backFace, int materialID)
        {
            Assert.IsTrue(vertexData.Length == 4);

            List<GeometryBuffer> holder = m_buffers[materialID];
            GeometryBuffer buffer = holder[holder.Count - 1];

            // If there are too many vertices we need to create a new separate buffer for them
            if (buffer.Vertices.Count + 4 > 65000)
            {
                buffer = new GeometryBuffer();
                holder.Add(buffer);
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

                for (int i = 0; i< holder.Count; i++)
                {
                    GeometryBuffer buffer = holder[i];

                    // No data means there's no mesh to build
                    if (buffer.IsEmpty())
                        continue;

                    // Create a game object for collider. Unfortunatelly, we can't use object pooling
                    // here for otherwise, unity would have to rebake because of change in object position
                    // and that is very time consuming.
                    GameObject prefab = GameObjectProvider.GetPool(m_prefabName).Prefab;
                    GameObject go = Object.Instantiate(prefab);
                    go.transform.parent = GameObjectProvider.Instance.ProviderGameObject.transform;

                    {
#if DEBUG
                        go.name = debugName+"_"+i;
#endif

                        Mesh mesh = Globals.MemPools.MeshPool.Pop();
                        Assert.IsTrue(mesh.vertices.Length<=0);
                        UnityMeshBuilder.BuildColliderMesh(mesh, buffer);

                        MeshCollider collider = go.GetComponent<MeshCollider>();
                        collider.sharedMesh = null;
                        collider.sharedMesh = mesh;
                        collider.transform.position = position;
                        collider.transform.rotation = rotation;
                        collider.sharedMaterial = (m_materials==null || m_materials.Length<1) ? null : m_materials[j];

                        m_objects.Add(go);
                        m_colliders.Add(collider);
                    }

                    buffer.Clear();
                }
            }
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