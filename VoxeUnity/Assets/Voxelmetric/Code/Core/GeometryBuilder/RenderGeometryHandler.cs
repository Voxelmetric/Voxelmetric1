using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Core
{
    public class RenderGeometryHandler: IChunkGeometryHandler
    {
        private Chunk chunk;

        public RenderGeometryBatcher Batcher { get; private set; }

        public RenderGeometryHandler(Chunk chunk)
        {
            this.chunk = chunk;
            Batcher = new RenderGeometryBatcher(this.chunk);
        }

        public void Reset()
        {
            Batcher.Clear();
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public void Build()
        {
            Globals.CubeMeshBuilder.Build(chunk);
        }

        public void Commit()
        {
            Batcher.Commit();
        }
    }
}