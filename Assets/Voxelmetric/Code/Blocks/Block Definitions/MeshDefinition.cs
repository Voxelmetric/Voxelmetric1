using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class MeshDefinition : BlockDefinition {

    public string meshName;
    public string texture;
    public bool[] blockIsSolid= new bool[6];

    public Vector3 positionOffset;

    public override BlockController Controller()
    {
        CustomMesh controller = new CustomMesh();
        controller.blockName = blockName;
        controller.isSolid = blockIsSolid;

        if (Application.isPlaying)
        {
            CustomMesh.SetUpMeshControllerMesh(meshName, controller, positionOffset);
            controller.collection = Block.index.textureIndex.GetTextureCollection(texture);
        }
        return controller;
    }

    
}
