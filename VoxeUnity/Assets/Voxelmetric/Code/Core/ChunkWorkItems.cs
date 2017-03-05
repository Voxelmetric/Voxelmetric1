using Voxelmetric.Code.Core.StateManager;

namespace Voxelmetric.Code.Core
{
    public struct SGenerateColliderWorkItem
    {
        public readonly ChunkStateManagerClient StateManager;
        public readonly int MinX;
        public readonly int MaxX;
        public readonly int MinY;
        public readonly int MaxY;
        public readonly int MinZ;
        public readonly int MaxZ;

        public SGenerateColliderWorkItem(ChunkStateManagerClient stateManager, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            StateManager = stateManager;
            MinX = minX;
            MaxX = maxX;
            MinY = minY;
            MaxY = maxY;
            MinZ = minZ;
            MaxZ = maxZ;
        }
    }
}
