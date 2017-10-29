using System.Collections;
using System.Globalization;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;

public class ConnectedMeshBlockConfig : CustomMeshBlockConfig
{
    public readonly int[][] directionalTris = new int[6][];
    public readonly Vector3[][] directionalVerts = new Vector3[6][];
    public readonly Vector2[][] directionalUVs = new Vector2[6][];
    public readonly Color32[][] directionalColors = new Color32[6][];
    public readonly TextureCollection[] directionalTextures = new TextureCollection[6];

    public int[] connectsToTypes;
    public string[] connectsToNames;
    public bool connectsToSolid;

    public override bool OnSetUp(Hashtable config, World world)
    {
        if (!base.OnSetUp(config, world))
            return false;
        
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
                meshOffset,
                out directionalTris[dir],
                out directionalVerts[dir],
                out directionalUVs[dir],
                out directionalColors[dir]
                );
        }

        return true;
    }
}
