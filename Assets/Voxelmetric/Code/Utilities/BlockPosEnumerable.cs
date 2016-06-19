using System.Collections;
using System.Collections.Generic;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public class BlockPosEnumerable : IEnumerable<Vector3Int> {

        private readonly Vector3Int start;
        private readonly Vector3Int end;
        private readonly Vector3Int step;

        public BlockPosEnumerable(Vector3Int end, bool includesEnd = false) :
            this(Vector3Int.zero, end, includesEnd) {
            }

        public BlockPosEnumerable(Vector3Int start, Vector3Int end, bool includesEnd = false) :
            this(start, end, Vector3Int.one, includesEnd) {
            }

        public BlockPosEnumerable(Vector3Int start, Vector3Int end, Vector3Int step, bool includesEnd = false) {
            this.start = start;
            this.end = end;
            this.step = step;
            if (includesEnd)
                this.end += step;
        }

        public IEnumerator<Vector3Int> GetEnumerator()
        {
            for (int y = start.y; y < end.y; y += step.y) {
                for (int z = start.z; z < end.z; z += step.z) {
                    for (int x = start.x; x < end.x; x += step.x)
                    {
                        yield return new Vector3Int(x, y, z);
                    }
                }
            }
        }

        IEnumerator IEnumerable.GetEnumerator() {
            return GetEnumerator();
        }
    }
}
