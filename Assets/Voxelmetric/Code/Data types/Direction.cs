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
    public const int NumDirections = 6;

    public static Direction Get(int i)
    {
        switch (i)
        {
            case 0:
                return Direction.north;
            case 1:
                return Direction.east;
            case 2:
                return Direction.south;
            case 3:
                return Direction.west;
            case 4:
                return Direction.up;
            case 5:
                return Direction.down;
            default:
                return Direction.up;
        }
    }

    public static int Get(Direction dir)
    {
        switch (dir)
        {
            case Direction.north:
                return 0;
            case Direction.east:
                return 1;
            case Direction.south:
                return 2;
            case Direction.west:
                return 3;
            case Direction.up:
                return 4;
            case Direction.down:
                return 5;
            default:
                return 4;
        }
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
                return Direction.up;
        }
    }
}