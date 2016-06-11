using UnityEngine.Assertions;

namespace Voxelmetric.Code.Data_types
{
    public enum Direction {
        north,
        east,
        south,
        west,
        up,
        down
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
            switch (dir)
            {
                case Direction.north:
                    return Direction.south;
                case Direction.east:
                    return Direction.west;
                case Direction.south:
                    return Direction.down;
                case Direction.west:
                    return Direction.east;
                case Direction.up:
                    return Direction.down;
                case Direction.down:
                    return Direction.up;
                default:
                    Assert.IsTrue(false);
                    return Direction.up;
            }
        }

        public static bool Backface(Direction dir)
        {
            switch (dir)
            {
                case Direction.north:
                case Direction.east:
                case Direction.up:
                    return false;
            }

            return true;
        }
}
}