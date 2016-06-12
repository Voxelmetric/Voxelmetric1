using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public static class Env
    {
        public const int ChunkPower = 5;
        public const int ChunkPower2 = 2*ChunkPower;
        public const int ChunkSize = 1 << ChunkPower;
        public const int ChunkMask = ChunkSize-1;
        public const float BlockSize = 1f;
        public const float BlockSizeInv = 1f/BlockSize;
        public const int ChunkVolume = ChunkSize*ChunkSize*ChunkSize;

        public static readonly BlockPos ChunkSizePos = BlockPos.one * ChunkSize;

        // Padding added to the size of block faces to fix floating point issues 
        // where tiny gaps can appear between block faces
        public const float BlockFacePadding = 0.0005f;
    }

    public static class Core
    {
        public static readonly bool UseMultiThreading = true;
    }

    public static class Directories
    {
        public static readonly string SaveFolder = "VoxelSaves";
    }

    public static class Toggle
    {
        public static readonly bool UseMultipleWorlds = false;        
    }

}
