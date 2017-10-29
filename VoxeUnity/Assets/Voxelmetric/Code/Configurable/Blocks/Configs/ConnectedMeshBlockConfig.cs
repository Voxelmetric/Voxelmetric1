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

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;
        
        connectsToNames = _GetPropertyFromConfig(config, "connectsToNames", "").Replace(" ", "").Split(',');
        connectsToSolid = _GetPropertyFromConfig(config, "connectsToSolid", true);

        for (int dir = 0; dir < 6; dir++)
        {
            var d = m_datas[dir] = new CustomMeshBlockData();

            Direction direction = DirectionUtils.Get(dir);
            if (_GetPropertyFromConfig(config, direction + "FileLocation", "") == "")
                continue;

            d.textures = world.textureProvider.GetTextureCollection(_GetPropertyFromConfig(config, direction+"Texture", ""));

            Vector3 meshOffset;
            meshOffset.x = float.Parse(_GetPropertyFromConfig(config, direction + "XOffset", "0"), CultureInfo.InvariantCulture);
            meshOffset.y = float.Parse(_GetPropertyFromConfig(config, direction + "YOffset", "0"), CultureInfo.InvariantCulture);
            meshOffset.z = float.Parse(_GetPropertyFromConfig(config, direction + "ZOffset", "0"), CultureInfo.InvariantCulture);
            
            SetUpMesh(
                world.config.meshFolder + "/" + _GetPropertyFromConfig(config, direction + "FileLocation", ""),
                meshOffset, out d.tris, out d.verts, out d.uvs, out d.colors
                );
        }

        return true;
    }
}
