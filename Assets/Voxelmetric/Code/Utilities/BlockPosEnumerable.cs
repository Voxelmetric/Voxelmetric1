using System.Collections;
using System.Collections.Generic;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public class BlockPosEnumerable : IEnumerable<BlockPos> {

        private readonly BlockPos start;
        private readonly BlockPos end;
        private readonly BlockPos step;

        public BlockPosEnumerable(BlockPos end, bool includesEnd = false) :
            this(BlockPos.zero, end, includesEnd) {
            }

        public BlockPosEnumerable(BlockPos start, BlockPos end, bool includesEnd = false) :
            this(start, end, BlockPos.one, includesEnd) {
            }

        public BlockPosEnumerable(BlockPos start, BlockPos end, BlockPos step, bool includesEnd = false) {
            this.start = start;
            this.end = end;
            this.step = step;
            if (includesEnd)
                this.end += step;
        }

        public IEnumerator<BlockPos> GetEnumerator()
        {
            for (int y = start.y; y < end.y; y += step.y) {
                for (int z = start.z; z < end.z; z += step.z) {
                    for (int x = start.x; x < end.x; x += step.x)
                    {
                        yield return new BlockPos(x, y, z);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
