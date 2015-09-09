using UnityEngine;
using System.Collections;

public class CrossMeshDefinition : BlockDefinition {

    public string texture;

    public override BlockController Controller()
    {
        BlockCrossMesh controller = new BlockCrossMesh();
        if(texture!="")
            controller.texture = Voxelmetric.resources.textureIndex.GetTextureCollection(texture);
        controller.blockName = blockName;
        return controller;
    }

}
