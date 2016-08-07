using Voxelmetric.Code.Rendering.GeometryHandler;

namespace Voxelmetric.Code.Core.GeometryHandler
{
    public class ChunkRenderGeometryHandler: ARenderGeometryHandler
    {
        private const string PoolEntryName = "Renderable";
        private readonly Chunk chunk;

        public ChunkRenderGeometryHandler(Chunk chunk): base(PoolEntryName)
        {
            this.chunk = chunk;
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public override void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            Globals.CubeMeshBuilder.Build(chunk, minX, maxX, minY, maxY, minZ, maxZ);
        }

        public override void Commit()
        {
            Batcher.Commit(
                chunk.world.transform.rotation*chunk.pos+chunk.world.transform.position,
                chunk.world.transform.rotation, chunk.world.renderMaterial
#if DEBUG
                , chunk.pos.ToString()
#endif
                );
        }
    }
}