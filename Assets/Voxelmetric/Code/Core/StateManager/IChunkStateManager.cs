namespace Voxelmetric.Code.Core.StateManager
{
    public interface IChunkStateManager
    {
        void Init();
        void Reset();

        bool CanUpdate();
        void Update();

        void RequestState(ChunkState state);

        bool IsStateCompleted(ChunkState state);
        bool IsSavePossible { get; }

        void SetMeshBuilt(); // temporary!
    }
}
