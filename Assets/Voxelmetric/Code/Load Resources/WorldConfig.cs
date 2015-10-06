using System;

public struct WorldConfig
{
    public string textureFolder;
    public string name;
    public string blockFolder;
    public bool useCustomTextureAtlas;
    public string customTextureAtlasFile;

    public override string ToString()
    {
        return name;
    }
}
