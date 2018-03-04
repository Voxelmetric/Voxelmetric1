using UnityEngine;

namespace Voxelmetric.Code.Load_Resources
{
    public struct WorldConfig
    {
        public string name;
        public string textureFolder;
        public string layerFolder;
        public string blockFolder;
        public string meshFolder;

        public int minX;
        public int maxX;
        public int maxY;
        public int minY;
        public int minZ;
        public int maxZ;

        public float randomUpdateFrequency;
        
        public bool addAOToMesh;
        public float ambientOcclusionStrength;
        
        // These variables relate to how the textures are loaded and how the atlas is created.
        // There is an issue in that texture indexes are created with these variables but if
        // you create a new world using the same texture folder the new world will use the existing
        // index with the settings of the world that created it.
        public int textureAtlasPadding;
        public FilterMode textureAtlasFiltering;
        public TextureFormat textureFormat;
        public bool useMipMaps;

        public override string ToString()
        {
            return name;
        }
    }
}
