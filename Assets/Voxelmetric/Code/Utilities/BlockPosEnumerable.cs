using System.Collections;
using System.Collections.Generic;

public class BlockPosEnumerable : IEnumerable<BlockPos> {

    private readonly BlockPos start;
    private readonly BlockPos end;
    private readonly BlockPos step;
    private readonly bool includesEnd;

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

    public IEnumerator<BlockPos> GetEnumerator() {
        for (int x = start.x; x < end.x; x += step.x) {
            for (int y = start.y; y < end.y; y += step.y) {
                for (int z = start.z; z < end.z; z += step.z) {
                    yield return new BlockPos(x, y, z);
                }
            }
        }
    }

    IEnumerator IEnumerable.GetEnumerator() {
        return GetEnumerator();
    }
}
