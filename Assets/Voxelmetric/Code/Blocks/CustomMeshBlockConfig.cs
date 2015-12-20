using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;

public class CustomMeshBlockConfig: BlockConfig
{
    public TextureCollection[] textures;

    public string meshFileName;
    public Vector3 meshOffset;

    public int[] tris;
    public Vector3[] verts;
    public Vector2[] uvs;

    public TextureCollection texture;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);

        texture = world.textureIndex.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        meshOffset.x = float.Parse(_GetPropertyFromConfig(config, "meshXOffset", "0"));
        meshOffset.y = float.Parse(_GetPropertyFromConfig(config, "meshYOffset", "0"));
        meshOffset.z = float.Parse(_GetPropertyFromConfig(config, "meshZOffset", "0"));

        SetUpMesh(world.config.meshFolder + "/" + _GetPropertyFromConfig(config, "meshFileLocation", ""), meshOffset);
    }

    public void SetUpMesh(string meshLocation, Vector3 positionOffset)
    {
        GameObject meshGO = (GameObject)Resources.Load(meshLocation);

        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < mesh.vertices.Length; i++)
            {
                verts.Add(mesh.vertices[i] + positionOffset);
            }

            for (int i = 0; i < mesh.triangles.Length; i++)
            {
                tris.Add(mesh.triangles[i]);
            }

            for (int i = 0; i < mesh.uv.Length; i++)
            {
                uvs.Add(mesh.uv[i]);
            }
        }

        this.tris = tris.ToArray();
        this.verts = verts.ToArray();
        this.uvs = uvs.ToArray();
    }
}
