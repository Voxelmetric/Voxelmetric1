namespace Voxelmetric.Code.Data_types
{
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
}