using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uv = new List<Vector2>();
    public List<Color> colors = new List<Color>();

    Vector3[] verticesArray;
    int[] trianglesArray;
    Vector2[] uvArray;
    Color[] colorsArray;

    public Mesh mesh;

    public MeshData() { }

    public void AddQuadTriangles()
    {
            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
    }

    public void AddVertex(Vector3 vertex)
    {
            vertices.Add(vertex);
    }

    public void AddTriangle(int tri)
    {
            triangles.Add(tri);
    }

    public void AddTriangle()
    {
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
    }

    public void AddMesh(int[] tris, Vector3[] verts, Vector2[] uvs, Rect texture, Vector3 offset)
    {
        int initialVertCount = vertices.Count;

        for (int i = 0; i < verts.Length; i++)
        {
            AddVertex(verts[i] + offset);

            if (uvs.Length == 0)
                uv.Add(new Vector2(0, 0));

            //Coloring of blocks is not yet implemented so just pass in full brightness
            colors.Add(new Color(1, 1, 1, 1));
        }

        for (int i = 0; i < uvs.Length; i++)
        {
            uv.Add(new Vector2(
                (uvs[i].x * texture.width) + texture.x,
                (uvs[i].y * texture.height) + texture.y)
            );
        }

        for (int i = 0; i < tris.Length; i++)
        {
            AddTriangle(tris[i] + initialVertCount);
        }
    }

    // Keeping this method even though we're not yet adding anything but 1, 1, 1, 1
    public void AddColors(float ne, float es, float sw, float wn, float light)
    {
        wn = (wn * light);
        ne = (ne * light);
        es = (es * light);
        sw = (sw * light);
        
        colors.Add(new Color(wn, wn, wn));
        colors.Add(new Color(ne, ne, ne));
        colors.Add(new Color(es, es, es));
        colors.Add(new Color(sw, sw, sw));
    }

    public void ConvertToArrays()
    {
        colorsArray = colors.ToArray();
        verticesArray = vertices.ToArray();
        trianglesArray = triangles.ToArray();
        uvArray = uv.ToArray();

        colors.Clear();
        vertices.Clear();
        triangles.Clear();
        uv.Clear();
    }

    public void CommitMesh()
    {
        Mesh newMesh = new Mesh();
        newMesh.vertices = verticesArray;
        newMesh.triangles = trianglesArray;
        newMesh.colors = colorsArray;
        newMesh.uv = uvArray;
        newMesh.RecalculateNormals();

        mesh = newMesh;

        colorsArray = null;
        verticesArray = null;
        trianglesArray = null;
        uvArray = null;
    }
}