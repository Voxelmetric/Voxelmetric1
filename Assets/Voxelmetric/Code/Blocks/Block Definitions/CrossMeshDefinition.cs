using UnityEngine;
using System.Collections;

public class CrossMeshDefinition : BlockDefenition {

    public string blockName;
    public Vector2 texture;

    public override BlockController Controller()
    {
        BlockCrossMesh controller = new BlockCrossMesh();
        controller.texture = new Tile((int)texture.x, (int)texture.y);
        controller.blockName = blockName;
        return controller;
    }

}
