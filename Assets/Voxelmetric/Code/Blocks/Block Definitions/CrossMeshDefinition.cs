using UnityEngine;
using System.Collections;

public class CrossMeshDefinition : BlockDefinition {

    public string blockName;
    public string texture;

    public override BlockController Controller()
    {
        BlockCrossMesh controller = new BlockCrossMesh();
        controller.texture = Block.index.textureIndex.GetTextureCollection(texture);
        controller.blockName = blockName;
        return controller;
    }

}
