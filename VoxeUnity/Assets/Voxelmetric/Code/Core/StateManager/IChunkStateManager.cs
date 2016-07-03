namespace Voxelmetric.Code.Core.StateManager
{
    public interface IChunkStateManager
    {
        void Init();
        void Reset();

        bool CanUpdate();
        void Update();

        void RequestState(ChunkState state);
        void ResetRequest(ChunkState state);

        bool IsStateCompleted(ChunkState state);
        bool IsSavePossible { get; }

        //! TODO: Get rid of this - bad design. It should stay only temporary
        void SetMeshBuilt();
        void SetColliderBuilt();
    }
}
