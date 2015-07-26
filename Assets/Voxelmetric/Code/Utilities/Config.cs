using UnityEngine;

namespace Config
{
    public static class Env
    {
        public static int ChunkLoadRadius = 8; //how many chunks to load in each direction
        public static int ChunkSize = 16;
        public static int WorldMaxY = 64;
        public static int WorldMinY = -64;

        public static float BlockSize = 1f;

        public static float UpdateFrequency = 0.1f;

        // Padding added to the size of block faces to fix floating point issues 
        // where tiny gaps can appear between block faces
        public static float BlockFacePadding = 0.0005f;

        //this is a sane minimum, below this chunks will start blinking in and out on edges
        //The distance is measured in blocks
        public static int DistanceToDeleteChunks = (int)(ChunkSize * ChunkLoadRadius * 1.5f);
        public static int WaitBetweenDeletes = 10;

        //Recommend setting this to at least 2 when LightSceneOnStart is enabled 
        public static int WaitBetweenChunkGen = 1;

        public static float AOStrength = 1f;
        public static float BlockLightStrength = 0f;
    }

    public static class Directories
    {
        public static string SaveFolder = "VoxelSaves";
        public static string BlockMeshFolder = "Meshes";
        public static string TextureFolder = "Textures";
        public static string ConnectedTextureFolder = "Connected Textures";
    }

    public static class Toggle
    {
        public static bool UseOldTerrainGen = false;
        public static bool UseCollisionMesh = true;

        //Lighting needs multithreading to work quickly
        public static bool BlockLighting = false;
        public static bool LightSceneOnStart = false;

        //Multithreading must be disabled on web builds
        public static bool UseMultiThreading = true;
    }

}
