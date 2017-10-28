using System.Collections;
using System.Globalization;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;

public class ConnectedMeshBlockConfig : BlockConfig
{
    public TextureCollection texture;

    public readonly int[][] directionalTris = new int[6][];
    public readonly Vector3[][] directionalVerts = new Vector3[6][];
    public readonly Vector2[][] directionalUVs = new Vector2[6][];
    public readonly TextureCollection[] directionalTextures = new TextureCollection[6];

    public int[] connectsToTypes;
    public string[] connectsToNames;
    public bool connectsToSolid;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;

        texture = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, "texture", ""));

        connectsToNames = _GetPropertyFromConfig(config, "connectsToNames", "").Replace(" ", "").Split(',');
        connectsToSolid = _GetPropertyFromConfig(config, "connectsToSolid", true);

        for (int dir = 0; dir < 6; dir++)
        {
            Direction direction = DirectionUtils.Get(dir);
            if (_GetPropertyFromConfig(config, direction + "FileLocation", "") == "")
                continue;

            directionalTextures[dir] = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, direction+"Texture", ""));

            Vector3 meshOffset;
            meshOffset.x = float.Parse(_GetPropertyFromConfig(config, direction + "XOffset", "0"), CultureInfo.InvariantCulture);
            meshOffset.y = float.Parse(_GetPropertyFromConfig(config, direction + "YOffset", "0"), CultureInfo.InvariantCulture);
            meshOffset.z = float.Parse(_GetPropertyFromConfig(config, direction + "ZOffset", "0"), CultureInfo.InvariantCulture);
            
            SetUpMesh(
                world.config.meshFolder + "/" + _GetPropertyFromConfig(config, direction + "FileLocation", ""),
                meshOffset, dir
                );
        }

        return true;
    }

    private void SetUpMesh(string meshLocation, Vector3 positionOffset, int index)
    {
        GameObject meshGO = (GameObject)Resources.Load(meshLocation);

        int vertexCnt = 0;
        int triangleCnt = 0;

        for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            vertexCnt += mesh.vertices.Length;
            triangleCnt += mesh.triangles.Length;
        }

        var tris =  directionalTris[index] = new int[triangleCnt];
        var verts = directionalVerts[index] = new Vector3[vertexCnt];
        var uvs = directionalUVs[index] = new Vector2[vertexCnt];

        int ti = 0, vi = 0;

        for (int GOIndex = 0; GOIndex < meshGO.transform.childCount; GOIndex++)
        {
            Mesh mesh = meshGO.transform.GetChild(GOIndex).GetComponent<MeshFilter>().sharedMesh;

            for (int i = 0; i < mesh.vertices.Length; i++, vi++)
            {
                verts[vi] = mesh.vertices[i] + positionOffset;
                uvs[vi] = mesh.uv.Length != 0 ? mesh.uv[i] : new Vector2();
                //Coloring of blocks is not yet implemented so just pass in full brightness
                //colorsOut[vi] = new Color32(255, 255, 255, 255);
            }

            for (int i = 0; i < mesh.triangles.Length; i++, ti++)
                tris[ti] = mesh.triangles[i];
        }
    }
}
