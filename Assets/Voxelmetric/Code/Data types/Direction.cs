namespace Voxelmetric.Code.Data_types
{
    /// <summary>
    /// Enum helping us to determine a direction
    /// </summary>
    public enum Direction
    {
        north = 0, // front face
        south = 1, // back face
        east = 2,
        west = 3,
        up = 4,
        down = 5
    }

    public static class DirectionUtils
    {
        public static Direction Get(int i)
        {
            return (Direction)i;
        }

        public static int Get(Direction dir)
        {
            return (int)dir;
        }

        public static Direction Opposite(Direction dir)
        {
            // Toogle the first bit to get to opposite direction
            return (Direction)(Get(dir) ^ 1);
        }

        public static bool Backface(Direction dir)
        {
            // The first bit signalizes backface
            return (Get(dir) & 1)==1;
        }
    }
}