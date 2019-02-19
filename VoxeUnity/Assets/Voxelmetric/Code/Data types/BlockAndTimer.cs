namespace Voxelmetric.Code.Data_types
{
    public struct BlockAndTimer
    {
        public Vector3Int pos;
        public float time;

        public BlockAndTimer(Vector3Int pos, float time)
        {
            this.pos = pos;
            this.time = time;
        }
    }
}