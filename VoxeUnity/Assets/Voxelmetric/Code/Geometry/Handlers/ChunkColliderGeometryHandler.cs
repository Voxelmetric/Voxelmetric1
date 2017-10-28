using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Geometry.GeometryHandler
{
    public class ChunkColliderGeometryHandler: AColliderGeometryHandler
    {
        private const string PoolEntryName = "Collidable";
        private readonly Chunk chunk;

        public ChunkColliderGeometryHandler(Chunk chunk, PhysicMaterial[] materials): base(PoolEntryName, materials)
        {
            this.chunk = chunk;
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public override void Build()
        {
            Globals.TerrainMeshColliderBuilder.Build(chunk);
        }

        public override void Commit()
        {
            Batcher.Commit(
                chunk.world.transform.rotation*chunk.pos+chunk.world.transform.position,
                chunk.world.transform.rotation
#if DEBUG
                , chunk.pos+"C"
#endif
                );
        }
    }
}