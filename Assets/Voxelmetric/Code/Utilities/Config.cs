using UnityEngine;

namespace Config
{
    public static class Env
    {
        public const int ChunkPower = 4;
        public const int ChunkSize = 1 << 4;
        public const float BlockSize = 1f;

        public static readonly BlockPos ChunkSizePos = BlockPos.one * ChunkSize;

        // Padding added to the size of block faces to fix floating point issues 
        // where tiny gaps can appear between block faces
        public const float BlockFacePadding = 0.0005f;
    }

    public static class Directories
    {
        public static readonly string SaveFolder = "VoxelSaves";
    }

    public static class Toggle
    {
        public static bool UseMultipleWorlds = false;
        
        //Multi threading must be disabled on web builds
        public static bool UseMultiThreadingDefault = true;
    }

}
