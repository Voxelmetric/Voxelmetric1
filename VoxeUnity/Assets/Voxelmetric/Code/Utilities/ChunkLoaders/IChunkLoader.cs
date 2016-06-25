using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    public interface IChunkLoader
    {
        void PreProcessChunks();
        void ProcessChunks();
        void PostProcessChunks();

        void ProcessChunk(Chunk chunk);
    }
}
