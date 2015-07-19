public enum Direction {
    north,
    east,
    south,
    west,
    up,
    down
}

public struct Tile {
    public int x;
    public int y;

    public Tile(int x, int y)
    {
        this.x = x;
        this.y = y;
    }
}

public struct BlockAndTimer
{
    public BlockPos pos;
    public float time;

    public BlockAndTimer(BlockPos pos, float time)
    {
        this.pos = pos;
        this.time = time;
    }
}