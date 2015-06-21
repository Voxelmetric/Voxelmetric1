using UnityEngine;
using System.Collections;

public class CubeDefinition : BlockDefenition
{
    public string blockName;
    public Vector2[] textures = new Vector2[6];

    public bool blockIsSolid;

    public override BlockController Controller()
    {
        BlockCube controller = new BlockCube();
        controller.blockName = blockName;
        controller.isSolid = blockIsSolid;
        controller.textures = textures;

        return controller;
    }
}
