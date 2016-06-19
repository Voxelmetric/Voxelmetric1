using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.StateManager;

namespace Assets.Voxelmetric.Code.Core.StateManager
{
    public interface IChunkStateManager
    {
        void Init();
        void Reset();

        bool CanUpdate();
        void Update();

        void RequestState(ChunkState state);

        bool IsStateCompleted(ChunkState state);

        void SetMeshBuilt(); // temporary!
    }
}
