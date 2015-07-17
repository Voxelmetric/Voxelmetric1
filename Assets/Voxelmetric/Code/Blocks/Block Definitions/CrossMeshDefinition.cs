using UnityEngine;
using System.Collections;

public class CrossMeshDefinition : BlockDefinition {

    public string texture;

    public override BlockController Controller()
    {
        BlockCrossMesh controller = new BlockCrossMesh();
        if(texture!="")
            controller.texture = Block.index.textureIndex.GetTextureCollection(texture);
        controller.blockName = blockName;
        return controller;
    }

}
