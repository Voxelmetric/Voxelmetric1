using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Common.Extensions
{
    public static class ChunkPoolItemStateExtension
    {
        public static ChunkPoolItemState Set(this ChunkPoolItemState state, ChunkPoolItemState flag)
        {
            return state|flag;
        }

        public static ChunkPoolItemState Reset(this ChunkPoolItemState state, ChunkPoolItemState flag)
        {
            return state&(~flag);
        }

        public static bool Check(this ChunkPoolItemState state, ChunkPoolItemState flag)
        {
            return (state & flag) != 0;
        }

        public static ChunkPoolItemState Reset(this ChunkPoolItemState state)
        {
            return ChunkPoolItemState.None;
        }
    }
}
