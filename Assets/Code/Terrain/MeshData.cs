using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class MeshData
{
    public List<Vector3> vertices = new List<Vector3>();
    public List<int> triangles = new List<int>();
    public List<Vector2> uv = new List<Vector2>();

    public List<Color> colors = new List<Color>();

    public List<Vector3> colVertices = new List<Vector3>();
    public List<int> colTriangles = new List<int>();


    public MeshData() { }

    public void AddQuadTriangles(bool collisionMesh = false)
    {
        if (!collisionMesh)
        {
            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);

            triangles.Add(vertices.Count - 4);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
        }
        else
        {
            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 3);
            colTriangles.Add(colVertices.Count - 2);

            colTriangles.Add(colVertices.Count - 4);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 1);
        }
    }

    public void AddVertex(Vector3 vertex, bool collisionMesh = false)
    {
        if (!collisionMesh){
            vertices.Add(vertex);
        }
        else
        {
            colVertices.Add(vertex);
        }

    }

    public void AddTriangle(int tri, bool collisionMesh = false)
    {
        if (!collisionMesh){
            triangles.Add(tri);
        }
        else
        {
            colTriangles.Add(tri);
        }
    }

    public void AddTriangle(bool collisionMesh = false)
    {
        if (!collisionMesh)
        {
            triangles.Add(vertices.Count - 3);
            triangles.Add(vertices.Count - 2);
            triangles.Add(vertices.Count - 1);
        }
        else
        {
            colTriangles.Add(colVertices.Count - 3);
            colTriangles.Add(colVertices.Count - 2);
            colTriangles.Add(colVertices.Count - 1);
        }
    }

    public void AddColors(float ne, float es, float sw, float wn, float light)
    {
        wn = (wn/2) + (light / 2);
        ne = (ne/2) + (light / 2);
        es = (es /2) + (light / 2);
        sw = (sw/2) + (light / 2);
        
        colors.Add(new Color(wn, wn, wn));
        colors.Add(new Color(ne, ne, ne));
        colors.Add(new Color(es, es, es));
        colors.Add(new Color(sw, sw, sw));
    }
}