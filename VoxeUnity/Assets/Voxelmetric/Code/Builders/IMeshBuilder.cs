using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders
{
    public interface IMeshBuilder
    {
        void Build(Chunk chunk, Side sideMask);
    }
}
