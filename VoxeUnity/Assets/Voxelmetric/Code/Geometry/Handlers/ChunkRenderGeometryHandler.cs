using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Geometry.GeometryHandler
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
            Globals.TerrainMeshBuilder.Build(chunk);
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