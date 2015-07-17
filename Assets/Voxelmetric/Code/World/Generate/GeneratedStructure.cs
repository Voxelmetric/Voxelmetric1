public class GeneratedStructure {

    public int posX = 0;
    public int negX = 0;

    public int posY = 0;
    public int negY = 0;

    public int posZ = 0;
    public int negZ = 0;

    public virtual void Build(World world, BlockPos chunkPos, BlockPos pos, TerrainLayer layer) { }

    //public bool ChunkContains(BlockPos chunkPos, BlockPos pos)
    //{
    //    //          fpy
    //    //           | fpz
    //    //           | /
    //    //           |/
    //    //   fnx-----x-------fpx
    //    //          /|
    //    //         / |
    //    //       fnz |
    //    //          fny


    //    int fpy = pos.y + posX;
    //    int fny = pos.y - negY;
    //    int fpx = pos.x + posX;
    //    int fnx = pos.x - negX;
    //    int fpz = pos.z + posZ;
    //    int fnz = pos.z - negZ;

    //    if (fpy < chunkPos.y)
    //        return false;

    //    if (fny > (chunkPos.y + Config.Env.ChunkSize))
    //        return false;

    //    if (fpx < chunkPos.x)
    //        return false;

    //    if (fnx > (chunkPos.x + Config.Env.ChunkSize))
    //        return false;

    //    if (fpz < chunkPos.z)
    //        return false;

    //    if (fnz > (chunkPos.z + Config.Env.ChunkSize))
    //        return false;

    //    return true;
    //}
}
