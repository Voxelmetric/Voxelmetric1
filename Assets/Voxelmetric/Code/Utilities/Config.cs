using UnityEngine;

namespace Config
{
    public static class Env
    {
        public static int ChunkSize = 16;
        public static float BlockSize = 1f;

        // Padding added to the size of block faces to fix floating point issues 
        // where tiny gaps can appear between block faces
        public static float BlockFacePadding = 0.0005f;
    }

    public static class Directories
    {
        public static string SaveFolder = "VoxelSaves";
    }

    public static class Toggle
    {
        public static bool UseMultipleWorlds = false;
        
        //Multi threading must be disabled on web builds
        public static bool UseMultiThreading = true;
    }

}
