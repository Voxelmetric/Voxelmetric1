using System.Runtime.CompilerServices;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Common.Extensions
{
    public static class ChunkStateExtension
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkState Set(this ChunkState state, ChunkState flag)
        {
            return state|flag;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkState Reset(this ChunkState state, ChunkState flag)
        {
            return state&(~flag);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static bool Check(this ChunkState state, ChunkState flag)
        {
            return (state & flag) != 0;
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ChunkState Reset(this ChunkState state)
        {
            return ChunkState.Idle;
        }
    }
}
