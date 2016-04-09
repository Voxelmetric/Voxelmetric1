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
        public static float BlockFacePadding = 0.0005f;
        public static Block Void = new Block(0);
        public static Block Air = new Block(1);
    }

    public static class Directories
    {
        public static string SaveFolder = "VoxelSaves";
    }

    public static class Toggle
    {
        public static bool UseMultipleWorlds = false;        
    }

}
