using UnityEngine;
using System.Collections;

public class CubeDefinition : BlockDefenition
{
    public string blockName;
    public Vector2[] textures = new Vector2[6];

    public bool BlockIsSolid;

    public override BlockController Controller()
    {
        BlockCube controller = new BlockCube();
        controller.blockName = blockName;
        controller.textures = textures;

        return controller;
    }
}
