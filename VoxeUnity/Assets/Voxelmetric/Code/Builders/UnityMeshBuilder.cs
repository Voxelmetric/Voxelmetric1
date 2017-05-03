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
            //Vector3[] normals = pools.Vector3ArrayPool.Pop(size);
            //Vector4[] tangents = pools.Vector4ArrayPool.Pop(size);

            // Fill buffers with data.
            // Due to the way the memory pools work we might have received more
            // data than necessary. This little overhead is well worth it, though.
            // Fill unused data with "zeroes"
            // TODO: Make it so that vertex count is known ahead of time
            for (int i = 0; i<size; i++)
                vertices[i] = buffer.Vertices[i].Vertex;
            for (int i = size; i<vertices.Length; i++)
                vertices[i] = Vector3.zero;

            for (int i = 0; i<size; i++)
                uvs[i] = buffer.Vertices[i].UV;
            for (int i = size; i<uvs.Length; i++)
                uvs[i] = Vector2.zero;

            for (int i = 0; i<size; i++)
                colors[i] = buffer.Vertices[i].Color;
            for (int i = size; i<colors.Length; i++)
                colors[i] = new Color32();

            /*for (int i = 0; i<size; i++)
                normals[i] = buffer.Vertices[i].Normal;
            for (int i = size; i<normals.Length; i++)
                normals[i] = Vector3.zero;

            for (int i = 0; i < size; i++)
              tangents[i] = buffer.Vertices[i].Tangent;
            for (int i = size; i<tangents.Length; i++)
              tangents[i] = Vector4.zero;*/

            // Prepare mesh
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;
            mesh.colors32 = colors;
            mesh.normals = null;//normals;
            mesh.tangents = null;//tangents;
            mesh.SetTriangles(buffer.Triangles, 0);
            mesh.RecalculateNormals();

            // Return memory back to pool
            pools.Vector3ArrayPool.Push(vertices);
            pools.Vector2ArrayPool.Push(uvs);
            pools.Color32ArrayPool.Push(colors);
            //pools.Vector3ArrayPool.Push(normals);
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
            // Due to the way the memory pools work we might have received more
            // data than necessary. This little overhead is well worth it, though.
            // Fill unused data with "zeroes"
            // TODO: Make it so that vertex count is known ahead of time
            for (int i = 0; i<size; i++)
                vertices[i] = buffer.Vertices[i].Vertex;
            for (int i = size; i<vertices.Length; i++)
                vertices[i] = Vector3.zero;

            // Prepare mesh
            mesh.vertices = vertices;
            mesh.uv = null;
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;
            mesh.colors32 = null;
            mesh.normals = null;
            mesh.tangents = null;
            mesh.SetTriangles(buffer.Triangles, 0);
            mesh.RecalculateNormals();

            // Return memory back to pool
            pool.Push(vertices);
        }
    }
}
