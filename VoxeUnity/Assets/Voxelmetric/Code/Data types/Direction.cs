namespace Voxelmetric.Code.Data_types
{
    /// <summary>
    /// Enum helping us to determine a direction
    /// </summary>
    public enum Direction
    {
        up = 0,
        down = 1,
        north = 2, // front face
        south = 3, // back face
        east = 4,
        west = 5,

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
            // Toogle the first bit to get the opposite direction
            return (Direction)(Get(dir) ^ 1);
        }

        public static bool IsBackface(Direction dir)
        {
            // The first bit signalizes a backface
            return (Get(dir) & 1)==1;
        }
    }
}