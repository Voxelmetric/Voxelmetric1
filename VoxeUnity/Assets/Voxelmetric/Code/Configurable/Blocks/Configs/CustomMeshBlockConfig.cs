using System.Collections;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CustomMeshBlockConfig: BlockConfig
{
    public int[] tris { get; private set; }
    public Vector3[] verts { get; private set; }
    public Vector2[] uvs { get; private set; }
    public Color32[] colors { get; private set; }
    public TextureCollection texture;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;
        
        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        Vector3 meshOffset;
        meshOffset.x = Env.BlockSizeHalf + float.Parse(_GetPropertyFromConfig(config, "meshXOffset", "0"), CultureInfo.InvariantCulture);
        meshOffset.y = Env.BlockSizeHalf + float.Parse(_GetPropertyFromConfig(config, "meshYOffset", "0"), CultureInfo.InvariantCulture);
        meshOffset.z = Env.BlockSizeHalf + float.Parse(_GetPropertyFromConfig(config, "meshZOffset", "0"), CultureInfo.InvariantCulture);

        SetUpMesh(
            world.config.meshFolder + "/" + _GetPropertyFromConfig(config, "meshFileLocation", ""),
            meshOffset
            );

        return true;
    }

    private void SetUpMesh(string meshLocation, Vector3 positionOffset)
    {
        // TODO: Why not simply holding a mesh object instead of creating all these arrays?
        GameObject meshGO = (GameObject)Resources.Load(meshLocation);

        int vertexCnt = 0;
        int triangleCnt = 0;

        for (int GOIndex = 0; GOIndex<meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            vertexCnt += mesh.vertices.Length;
            triangleCnt += mesh.triangles.Length;
        }

        // 6 indices & 4 vertices per quad
        Assert.IsTrue((vertexCnt * 3)>>1==triangleCnt);
        if ((vertexCnt * 3)>>1!=triangleCnt)
        {
            Resources.UnloadAsset(meshGO);
            return;
        }

        tris = new int[triangleCnt];
        verts = new Vector3[vertexCnt];
        // TODO: Only allocate these if necessary
        uvs = new Vector2[vertexCnt];
        colors = new Color32[vertexCnt];

        int ti=0, vi=0;

        for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < mesh.vertices.Length; i++, vi++)
            {
                verts[vi] = mesh.vertices[i]+positionOffset;
                uvs[vi] = mesh.uv.Length!=0 ? mesh.uv[i] : new Vector2();
                colors[vi] = mesh.colors32.Length!=0 ? mesh.colors32[i] : new Color32(255, 255, 255, 255);
            }

            for (int i = 0; i < mesh.triangles.Length; i++, ti++)
                tris[ti] = mesh.triangles[i];
        }

        //Resources.UnloadAsset(meshGO);
    }
}
