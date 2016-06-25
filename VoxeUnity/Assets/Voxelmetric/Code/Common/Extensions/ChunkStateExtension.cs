using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Common.Extensions
{
    public static class ChunkStateExtension
    {
        public static ChunkState Set(this ChunkState state, ChunkState flag)
        {
            return state|flag;
        }

        public static ChunkState Reset(this ChunkState state, ChunkState flag)
        {
            return state&(~flag);
        }

        public static bool Check(this ChunkState state, ChunkState flag)
        {
            return (state & flag) != 0;
        }

        public static ChunkState Reset(this ChunkState state)
        {
            return ChunkState.Idle;
        }
    }
}
