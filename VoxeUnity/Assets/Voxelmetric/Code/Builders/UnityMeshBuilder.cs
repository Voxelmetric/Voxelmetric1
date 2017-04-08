using UnityEngine;
using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Builders
{
    public static class UnityMeshBuilder
    {
        /// <summary>
        ///     Copy render geometry data to a Unity mesh
        /// </summary>
        public static void BuildGeometryMesh(Mesh mesh, GeometryBuffer buffer)
        {
            int size = buffer.Vertices.Count;
            var pools = Globals.MemPools;

            // Avoid allocations by retrieving buffers from the pool
            Vector3[] vertices = pools.Vector3ArrayPool.Pop(size);
            Vector2[] uvs = pools.Vector2ArrayPool.Pop(size);
            Color32[] colors = pools.Color32ArrayPool.Pop(size);
            Vector3[] normals = pools.Vector3ArrayPool.Pop(size);
            //Vector4[] tangents = pools.Vector4ArrayPool.Pop(size);

            // Fill buffers with data
            for (int i = 0; i<size; i++)
            {
                VertexDataFixed vertexData = buffer.Vertices[i];
                vertices[i] = vertexData.Vertex;
                uvs[i] = vertexData.UV;
                colors[i] = vertexData.Color;
                normals[i] = vertexData.Normal;
                //tangents[i] = vertexData.Tangent;
            }

            // Due to the way the memory pools work we might have received more
            // data than necessary. This little overhead is well worth it, though.
            // Fill unused data with "zeroes"
            for (int i = size; i<vertices.Length; i++)
            {
                vertices[i] = Vector3.zero;
                uvs[i] = Vector2.zero;
                colors[i] = Color.clear;
                normals[i] = Vector3.zero;
                //tangents[i] = Vector4.zero;
            }

            // Prepare mesh
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.colors32 = colors;
            mesh.normals = normals;
            mesh.tangents = null;//tangents;
            mesh.SetTriangles(buffer.Triangles, 0);
            mesh.RecalculateNormals();

            // Return memory back to pool
            pools.Vector3ArrayPool.Push(vertices);
            pools.Vector2ArrayPool.Push(uvs);
            pools.Color32ArrayPool.Push(colors);
            pools.Vector3ArrayPool.Push(normals);
            //pools.Vector4ArrayPool.Push(tangents);
        }

        /// <summary>
        ///     Copy collider geometry data to a Unity mesh
        /// </summary>
        public static void BuildColliderMesh(Mesh mesh, GeometryBuffer buffer)
        {
            int size = buffer.Vertices.Count;
            var pool = Globals.MemPools.Vector3ArrayPool;

            // Avoid allocations by retrieving buffers from the pool
            Vector3[] vertices = pool.Pop(size);

            // Fill buffers with data
            for (int i = 0; i < size; i++)
            {
                VertexDataFixed vertexData = buffer.Vertices[i];
                vertices[i] = vertexData.Vertex;
            }

            // Due to the way the memory pools work we might have received more
            // data than necessary. This little overhead is well worth it, though.
            // Fill unused data with "zeroes"
            for (int i = size; i < vertices.Length; i++)
            {
                vertices[i] = Vector3.zero;
            }

            // Prepare mesh
            mesh.vertices = vertices;
            mesh.SetTriangles(buffer.Triangles, 0);

            // Return memory back to pool
            pool.Push(vertices);
        }
    }
}
