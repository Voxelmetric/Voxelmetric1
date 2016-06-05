using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ConnectedMeshBlockConfig : CustomMeshBlockConfig
{
    public Dictionary<Direction, int[]> directionalTris = new Dictionary<Direction, int[]>();
    public Dictionary<Direction, Vector3[]> directionalVerts = new Dictionary<Direction, Vector3[]>();
    public Dictionary<Direction, Vector2[]> directionalUvs = new Dictionary<Direction, Vector2[]>();
    public Dictionary<Direction, TextureCollection> directionalTextures = new Dictionary<Direction, TextureCollection>();

    public int[] connectsToTypes;
    public string[] connectsToNames;
    public bool connectsToSolid;

    public override void SetUp(Hashtable config, World world)
    {
        base.SetUp(config, world);

        connectsToNames = _GetPropertyFromConfig(config, "connectsToNames", "").Replace(" ", "").Split(',');
        connectsToSolid = _GetPropertyFromConfig(config, "connectsToSolid", true);

        for (int dir = 0; dir < 6; dir++)
        {
            Direction direction = DirectionUtils.Get(dir);
            if (_GetPropertyFromConfig(config, direction + "FileLocation", "") == "")
            {
                continue;
            }

            directionalTextures.Add(direction, world.textureIndex.GetTextureCollection(_GetPropertyFromConfig(config, direction + "Texture", "")));

            Vector3 offset;
            offset.x = float.Parse(_GetPropertyFromConfig(config, direction + "XOffset", "0"));
            offset.y = float.Parse(_GetPropertyFromConfig(config, direction + "YOffset", "0"));
            offset.z = float.Parse(_GetPropertyFromConfig(config, direction + "ZOffset", "0"));

            int[] newTris;
            Vector3[] newVerts;
            Vector2[] newUvs;
            string meshLocation = world.config.meshFolder + "/" + _GetPropertyFromConfig(config, direction + "FileLocation", "");

            SetUpMesh(meshLocation, offset, out newTris, out newVerts, out newUvs);

            directionalTris.Add(direction, newTris);
            directionalVerts.Add(direction, newVerts);
            directionalUvs.Add(direction, newUvs);
        }

    }
}
