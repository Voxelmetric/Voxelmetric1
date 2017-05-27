using UnityEngine;
using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Builders
{
    public static class UnityMeshBuilder
    {
        /// <summary>
        ///     Copy render geometry data to a Unity mesh
        /// </summary>
        public static void BuildGeometryMesh(Mesh mesh, GeometryBuffer buffer, bool useColors, bool useTextures, bool useTangents)
        {
            int size = buffer.Vertices.Count;
            var pools = Globals.MemPools;

            // Avoid allocations by retrieving buffers from the pool
            Vector3[] vertices = pools.Vector3ArrayPool.Pop(size);
            Vector2[] uvs = useTextures ? pools.Vector2ArrayPool.Pop(size) : null;
            Color32[] colors = useColors ? pools.Color32ArrayPool.Pop(size) : null;
            Vector4[] tangents = useTangents ? pools.Vector4ArrayPool.Pop(size) : null;

            // Fill buffers with data.
            // Due to the way the memory pools work we might have received more
            // data than necessary. This little overhead is well worth it, though.
            // Fill unused data with "zeroes"
            // TODO: Make it so that vertex count is known ahead of time
            for (int i = 0; i<size; i++)
                vertices[i] = buffer.Vertices[i].Vertex;
            for (int i = size; i<vertices.Length; i++)
                vertices[i] = Vector3.zero;

            if (useTextures)
            {
                for (int i = 0; i < size; i++)
                    uvs[i] = buffer.Vertices[i].UV;
                for (int i = size; i < uvs.Length; i++)
                    uvs[i] = Vector2.zero;
            }

            if (useColors)
            {
                for (int i = 0; i<size; i++)
                    colors[i] = buffer.Vertices[i].Color;
                for (int i = size; i<colors.Length; i++)
                    colors[i] = new Color32();
            }

            /*if (useTangents)
            {
                for (int i = 0; i<size; i++)
                    tangents[i] = buffer.Vertices[i].Tangent;
                for (int i = size; i<tangents.Length; i++)
                    tangents[i] = Vector4.zero;
            }*/

            // Prepare mesh
            mesh.vertices = vertices;
            mesh.uv = uvs;
            mesh.uv2 = null;
            mesh.uv3 = null;
            mesh.uv4 = null;
            mesh.colors32 = colors;
            mesh.normals = null;
            mesh.tangents = tangents;
            mesh.SetTriangles(buffer.Triangles, 0);
            mesh.RecalculateNormals();

            // Return memory back to pool
            pools.Vector3ArrayPool.Push(vertices);
            if (useTextures)
                pools.Vector2ArrayPool.Push(uvs);
            if (useColors)
                pools.Color32ArrayPool.Push(colors);
            if (useTangents)
                pools.Vector4ArrayPool.Push(tangents);
        }
    }
}
