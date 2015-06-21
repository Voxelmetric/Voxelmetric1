using UnityEngine;
using System.Collections;
#if UNITY_EDITOR
using UnityEditor;
#endif

[ExecuteInEditMode]
public class CustomMeshDefinition : BlockDefenition {

    public string meshAssetName;
    public string blockName = "custom block";
    public Vector3 positionOffset;

    public Vector3[] verts = new Vector3[0];
    public int[] tris = new int[0];
    public Vector2[] uvs = new Vector2[0];

    public bool build = false;

#if UNITY_EDITOR
    void Update()
    {
        if (build)
        {
            Mesh blockMesh = MeshFromAsset(meshAssetName);
            verts = new Vector3[blockMesh.vertices.Length];

            for(int i =0; i< blockMesh.vertices.Length; i++)
            {
                verts[i] = blockMesh.vertices[i] + positionOffset;
            }
            tris = blockMesh.triangles;
            uvs = blockMesh.uv;
            build = false;
        }
    }

    static Mesh MeshFromAsset(string assetName)
    {
        string assetPath = Config.Directories.BlockMeshFolder + assetName;
        Mesh mesh = AssetDatabase.LoadAssetAtPath(assetPath, typeof(Mesh)) as Mesh;

        return mesh;
    }
#endif

    public override BlockController Controller()
    {
        CustomMesh controller = new CustomMesh();

        controller.verts = verts;
        controller.tris = tris;
        controller.uvs = uvs;

        controller.blockName = blockName;

        return controller;
    }
}
