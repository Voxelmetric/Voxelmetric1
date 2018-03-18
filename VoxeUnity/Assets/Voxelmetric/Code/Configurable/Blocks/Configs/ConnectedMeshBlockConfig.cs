using System.Collections;
using System.Globalization;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

public class ConnectedMeshBlockConfig : CustomMeshBlockConfig
{
    public int[] connectsToTypes;
    public string[] connectsToNames;
    public bool connectsToSolid;

    private readonly CustomMeshBlockData[] m_datas = new CustomMeshBlockData[6];
    public CustomMeshBlockData[] dataDir { get { return m_datas; } }

    private class MeshBlockInfo
    {
        public string fileLocation;
        public Vector3 meshOffset;
    }
    private readonly MeshBlockInfo[] m_info = new MeshBlockInfo[6];

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;
        
        connectsToNames = _GetPropertyFromConfig(config, "connectsToNames", "").Replace(" ", "").Split(',');
        connectsToSolid = _GetPropertyFromConfig(config, "connectsToSolid", true);

        for (int dir = 0; dir < 6; dir++)
        {
            var d = m_datas[dir] = new CustomMeshBlockData();
            var i = m_info[dir] = new MeshBlockInfo();
            Direction direction = DirectionUtils.Get(dir);

            i.fileLocation = _GetPropertyFromConfig(config, direction+"FileLocation", "");
            if (string.IsNullOrEmpty(i.fileLocation))
                continue;

            string textureName = _GetPropertyFromConfig(config, direction+"Texture", "");
            d.textures = world.textureProvider.GetTextureCollection(textureName);

            i.meshOffset = new Vector3(
                float.Parse(_GetPropertyFromConfig(config, direction+"XOffset", "0"), CultureInfo.InvariantCulture),
                float.Parse(_GetPropertyFromConfig(config, direction+"YOffset", "0"), CultureInfo.InvariantCulture),
                float.Parse(_GetPropertyFromConfig(config, direction+"ZOffset", "0"), CultureInfo.InvariantCulture)
            );
        }

        return true;
    }

    public override bool OnPostSetUp(World world)
    {
        if (!base.OnPostSetUp(world))
            return false;

        for (int dir = 0; dir<6; dir++)
        {
            var d = m_datas[dir];
            var i = m_info[dir];

            if (string.IsNullOrEmpty(i.fileLocation))
                continue;
            
            string meshLocation = world.config.meshFolder + "/" + i.fileLocation;
            SetUpMesh(
                world, meshLocation, type, i.meshOffset, m_scale,
                out d.tris, out d.verts, out d.uvs, out d.colors
            );
        }

        return true;
    }
}
