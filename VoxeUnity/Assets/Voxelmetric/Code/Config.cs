namespace Voxelmetric.Code
{
    public static class Env
    {
        public const int ChunkPow = 5;
        public const int ChunkPow2 = ChunkPow<<1;
        public const int ChunkMask = (1<<ChunkPow)-1;

        public const float BlockSize = 1f;
        public const float BlockSizeInv = 1f/BlockSize;

        // Padding added to the size of block faces to fix floating point issues
        // where tiny gaps can appear between block faces
        public const float BlockFacePadding = 0.0005f;

        #region DO NOT CHANGE THESE!

        //! Internal chunk size including room for edge fields as well so that we do not have to check whether we are within chunk bounds.
        //! This means we will ultimately consume a bit more memory in exchange for more performance
        public const int ChunkPadding = 1; // Do not change this value

        //! Visible chunk size
        public const int ChunkSize = (1<<ChunkPow)-2*ChunkPadding;
        public const int ChunkSize1 = ChunkSize-1;
        public const int ChunkSizePow2 = ChunkSize*ChunkSize;
        public const int ChunkSizePow3 = ChunkSize*ChunkSizePow2;
        
        //! Internal chunk size (visible size + padding)
        public const int ChunkSizePlusPadding = ChunkSize+ChunkPadding;
        public const int ChunkSizeWithPadding = ChunkSize+ChunkPadding*2;
        public const int ChunkSizeWithPaddingPow2 = ChunkSizeWithPadding*ChunkSizeWithPadding;
        public const int ChunkSizeWithPaddingPow3 = ChunkSizeWithPadding*ChunkSizeWithPaddingPow2;

        #endregion
    }

    public static class Features
    {
        public const bool UseThreadPool = true;
        public const bool UseThreadedIO = true;

        //! If true, chunk serialization is enabled
        public const bool UseSerialization = true;
        //! If true, chunk will be serialized when it's unloaded
        public const bool SerializeChunkWhenUnloading = true;
        //! If true, only difference form default-generated data will be stored
        //! If there is no change no serialization is performned unless UseDifferentialSerialization_ForceSaveHeaders is enabled
        public const bool UseDifferentialSerialization = true;
        //! If true, even if there is no difference in data, at least basic info about chunk structure is stored
        public const bool UseDifferentialSerialization_ForceSaveHeaders = false;
    }

    public static class Directories
    {
        public const string SaveFolder = "VoxelSaves";
    }
}
