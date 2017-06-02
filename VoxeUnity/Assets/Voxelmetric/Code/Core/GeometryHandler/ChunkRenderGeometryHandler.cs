using UnityEngine;
using Voxelmetric.Code.Geometry.GeometryHandler;

namespace Voxelmetric.Code.Core.GeometryHandler
{
    public class ChunkRenderGeometryHandler: ARenderGeometryHandler
    {
        private const string PoolEntryName = "Renderable";
        private readonly Chunk chunk;

        public ChunkRenderGeometryHandler(Chunk chunk, Material[] materials): base(PoolEntryName, materials)
        {
            this.chunk = chunk;
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public override void Build()
        {
            Globals.CubeMeshBuilder.Build(chunk, Features.DontRenderWorldEdgesMask);
        }

        public override void Commit()
        {
            Batcher.Commit(
                chunk.world.transform.rotation*chunk.pos+chunk.world.transform.position,
                chunk.world.transform.rotation
#if DEBUG
                , chunk.pos.ToString()
#endif
                );
        }
    }
}