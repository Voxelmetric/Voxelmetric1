using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders
{
    public abstract class AMeshBuilder
    {
        public Side SideMask { get; set; }
        public abstract void Build(Chunk chunk);
    }
}
