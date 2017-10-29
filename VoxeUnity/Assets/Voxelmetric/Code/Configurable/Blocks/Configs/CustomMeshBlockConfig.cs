using System.Collections;
using System.Globalization;
using NUnit.Framework;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;

public class CustomMeshBlockConfig: BlockConfig
{
    public int[] tris { get { return m_tris; } }
    public Vector3[] verts{ get { return m_verts; } }
    public Vector2[] uvs{ get { return m_uvs; } }
    public Color32[] colors { get { return m_colors; } }
    
    private int[] m_tris;
    private Vector3[] m_verts;
    private Vector2[] m_uvs;
    private Color32[] m_colors;

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
            meshOffset,
            out m_tris,
            out m_verts,
            out m_uvs,
            out m_colors
            );

        return true;
    }

    protected static void SetUpMesh(
        string meshLocation,
        Vector3 positionOffset,
        out int[] trisOut,
        out Vector3[] vertsOut,
        out Vector2[] uvsOut,
        out Color32[] colorsOut
        )
    {
        // TODO: Why not simply holding a mesh object instead of creating all these arrays?
        GameObject meshGO = (GameObject)Resources.Load(meshLocation);

        trisOut = null;
        vertsOut = null;
        uvsOut = null;
        colorsOut = null;

        int vertexCnt = 0;
        int triangleCnt = 0;

        bool hasColors = false;
        bool hasUVs = false;

        for (int GOIndex = 0; GOIndex<meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            vertexCnt += mesh.vertices.Length;
            triangleCnt += mesh.triangles.Length;

            // Check whether allocating space for UVs is necessary
            if (!hasUVs && mesh.uv != null && mesh.uv.Length > 0)
                hasUVs = true;

            // Check whether allocating space for colors is necessary
            if (!hasColors && mesh.colors32 != null && mesh.colors32.Length > 0)
                hasColors = true;
        }

        // 6 indices & 4 vertices per quad
        Assert.IsTrue((vertexCnt * 3)>>1==triangleCnt);
        if ((vertexCnt * 3)>>1!=triangleCnt)
        {
            // A bad resource
            Debug.LogErrorFormat("Error loading mesh {0}. Number of triangles and vertices do not match!", meshLocation);
            return;
        }

        trisOut = new int[triangleCnt];
        vertsOut = new Vector3[vertexCnt];
        if (hasUVs)
            uvsOut = new Vector2[vertexCnt];
        if (hasColors)
            colorsOut = new Color32[vertexCnt];

        int ti=0, vi=0;

        for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < mesh.vertices.Length; i++, vi++)
            {
                vertsOut[vi] = mesh.vertices[i]+positionOffset;

                if (hasUVs)
                    uvsOut[vi] = mesh.uv.Length!=0 ? mesh.uv[i] : new Vector2();

                if (hasColors)
                    colorsOut[vi] = mesh.colors32.Length!=0 ? mesh.colors32[i] : new Color32(255, 255, 255, 255);
            }

            for (int i = 0; i < mesh.triangles.Length; i++, ti++)
                trisOut[ti] = mesh.triangles[i];
        }
    }
}
