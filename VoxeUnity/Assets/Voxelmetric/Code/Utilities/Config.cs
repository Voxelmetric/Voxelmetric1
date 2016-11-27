using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public static class Env
    {
        public static readonly int ChunkPow = 5;
        public static readonly int ChunkPow2 = 2*ChunkPow;
        public static readonly int ChunkSize = 1 << ChunkPow;
        public static readonly int ChunkSizePow2 = ChunkSize*ChunkSize;
        public static readonly int ChunkSizePow3 = ChunkSize*ChunkSizePow2;
        public static readonly int ChunkMask = ChunkSize-1;


        //! Internal chunk size including room for edge fields as well so that we do not have to check whether we are within chunk bounds.
        //! This means we will ultimately consume a bit more memory in exchange for more performance
        public static readonly int ChunkPadding = 1;
        public static readonly int ChunkSizePlusPadding = ChunkSize + ChunkPadding;
        public static readonly int ChunkSizeWithPadding = ChunkSize + ChunkPadding * 2;
        public static readonly int ChunkSizeWithPaddingPow2 = ChunkSizeWithPadding*ChunkSizeWithPadding;
        public static readonly int ChunkSizeWithPaddingPow3 = ChunkSizeWithPadding*ChunkSizeWithPaddingPow2;

        public static readonly float BlockSize = 1f;
        public static readonly float BlockSizeInv = 1f / BlockSize;
        public static readonly Vector3Int ChunkSizePos = Vector3Int.one * ChunkSize;

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
}
