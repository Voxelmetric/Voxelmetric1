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

        //Recommend setting this to at least 1 when LightSceneOnStart is enabled 
        public static int WaitBetweenChunkGen = 0;
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

    public static class Textures
    {
        public static Tile Stone = new Tile(0, 0);
        public static Tile Dirt = new Tile(1, 0);
        public static Tile Grass = new Tile(2, 0);
        public static Tile GrassSide = new Tile(3, 0);
        public static Tile LogSide = new Tile(1, 1);
        public static Tile LogTop = new Tile(2, 1);
        public static Tile Leaves = new Tile(0, 1);
        public static Tile Sand = new Tile(3, 2);
        public static Tile WildGrass = new Tile(4, 0);
    }

}
