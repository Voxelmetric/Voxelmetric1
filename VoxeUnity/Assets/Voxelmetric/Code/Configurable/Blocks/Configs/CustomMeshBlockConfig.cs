using UnityEngine;
using System.Collections;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

public class CustomMeshBlockConfig: BlockConfig
{
    public TextureCollection[] textures;

    public int[] tris { get { return m_triangles; } }
    public VertexDataFixed[] verts { get { return m_vertices; } }

    public TextureCollection texture;

    private int[] m_triangles;
    private VertexDataFixed[] m_vertices;

    public override bool SetUp(Hashtable config, World world)
    {
        if (!base.SetUp(config, world))
            return false;

        solid = _GetPropertyFromConfig(config, "solid", false);
        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        Vector3 meshOffset;
        meshOffset.x = float.Parse(_GetPropertyFromConfig(config, "meshXOffset", "0"));
        meshOffset.y = float.Parse(_GetPropertyFromConfig(config, "meshYOffset", "0"));
        meshOffset.z = float.Parse(_GetPropertyFromConfig(config, "meshZOffset", "0"));

        SetUpMesh(world.config.meshFolder + "/" + _GetPropertyFromConfig(config, "meshFileLocation", ""), meshOffset, out m_triangles, out m_vertices);


        return true;
    }

    protected static void SetUpMesh(string meshLocation, Vector3 positionOffset, out int[] trisOut, out VertexDataFixed[] vertsOut)
    {
        GameObject meshGO = (GameObject)Resources.Load(meshLocation);

        int vertexCnt = 0;
        int triangleCnt = 0;

        for (int GOIndex = 0; GOIndex<meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            vertexCnt += mesh.vertices.Length;
            triangleCnt += mesh.triangles.Length;
        }

        trisOut = new int[triangleCnt];
        vertsOut = new VertexDataFixed[vertexCnt];

        int ti=0, vi=0;

        for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < mesh.vertices.Length; i++, vi++)
            {
                vertsOut[vi] = new VertexDataFixed
                {
                    Vertex = mesh.vertices[i]+positionOffset,
                    UV = mesh.uv.Length!=0 ? mesh.uv[i] : new Vector2(),
                    //Coloring of blocks is not yet implemented so just pass in full brightness
                    Color = new Color32(255, 255, 255, 255)
                };
            }

            for (int i = 0; i < mesh.triangles.Length; i++, ti++)
                trisOut[ti] = mesh.triangles[i];
        }
    }
}
