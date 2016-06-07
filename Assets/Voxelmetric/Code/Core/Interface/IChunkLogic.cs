using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Interface
{
    internal interface IChunkLogic
    {
        void Reset();

        void TimedUpdated();

        void AddScheduledUpdate(BlockPos pos, float time);
    }
}
