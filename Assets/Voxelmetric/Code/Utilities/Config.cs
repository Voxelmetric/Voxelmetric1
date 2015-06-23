namespace Config
{
    public static class Env
    {
        public static float TileSize = 0.125f; // (1/8) for a tile sheet of 8 x 8 tiles
        public static int ChunkSize = 16;
        public static int WorldMaxY = 64;
        public static int WorldMinY = -64;

        public static int ChunksToLoad = Data.chunkLoadOrder.Length;
        public static int DistanceToDeleteChunks = 256;
        public static int WaitBetweenDeletes = 10;

        //Recommend setting this to at least 2 when LightSceneOnStart is enabled 
        public static int WaitBetweenChunkGen = 0;

        public static float AOStrength = 0.5f;
        public static float BlockLightStrength = 0.5f;
    }

    public static class Directories
    {
        public static string SaveFolder = "VoxelSaves";
        public static string BlockMeshFolder = "Assets/Voxelmetric/BlockMeshes/";
    }

    public static class Toggle
    {
        public static bool UseCollisionMesh = true;

        //Lighting needs multithreading to work quickly
        public static bool BlockLighting = false;
        public static bool LightSceneOnStart = false;

        //Multithreading must be disabled on web builds
        public static bool UseMultiThreading = true;
    }

}
