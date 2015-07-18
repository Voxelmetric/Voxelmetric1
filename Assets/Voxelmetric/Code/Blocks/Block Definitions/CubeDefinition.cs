using UnityEngine;
using System.Collections;

public class CubeDefinition : BlockDefinition
{
    public string blockName;
    public string[] textures = new string[6];

    public bool blockIsSolid = true;
    public bool solidTowardsSameType = true;

    public override BlockController Controller()
    {
        BlockCube controller = new BlockCube();
        controller.blockName = blockName;
        controller.isSolid = blockIsSolid;
        controller.solidTowardsSameType = solidTowardsSameType;

        TextureCollection[] textureCoordinates = new TextureCollection[6];

        for (int i = 0; i < 6; i++)
        {
            textureCoordinates[i] = Block.index.textureIndex.GetTextureCollection(textures[i]);
        }

        controller.textures = textureCoordinates;

        return controller;
    }
}
