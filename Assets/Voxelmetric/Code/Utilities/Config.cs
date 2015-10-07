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


        //// These should be settings on chunk gen
        //public static int ChunkLoadRadius = 8; //how many chunks to load in each direction
        ////this is a sane minimum, below this chunks will start blinking in and out on edges
        ////The distance is measured in blocks
        //public static int DistanceToDeleteChunks = (int)(ChunkSize * ChunkLoadRadius * 1.5f);
        //public static int WaitBetweenDeletes = 10;
        ////Recommend setting this to at least 2 when LightSceneOnStart is enabled 
        //public static int WaitBetweenChunkGen = 1;
    }

    public static class Directories
    {
        public static string SaveFolder = "VoxelSaves";
    }

    public static class Toggle
    {
        public static bool UseMultipleWorlds = true;
        
        //Multi threading must be disabled on web builds
        public static bool UseMultiThreading = true;
    }

}
