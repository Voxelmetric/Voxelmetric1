using Voxelmetric.Code.Rendering.GeometryHandler;

namespace Voxelmetric.Code.Core.GeometryHandler
{
    public class ChunkColliderGeometryHandler: AColliderGeometryHandler
    {
        private readonly Chunk chunk;

        public ChunkColliderGeometryHandler(Chunk chunk)
        {
            this.chunk = chunk;
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public override void Build(int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            Globals.CubeMeshColliderBuilder.Build(chunk, minX, maxX, minY, maxY, minZ, maxZ);
        }

        public override void Commit()
        {
            Batcher.Commit(
                chunk.world.transform.rotation * chunk.pos + chunk.world.transform.position,
                chunk.world.transform.rotation, chunk.world.physicsMaterial
                );
        }
    }
}