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
            Globals.TerrainMeshColliderBuilder.Build(chunk, out chunk.MinBoundsC, out chunk.MaxBoundsC);
        }

        public override void Commit()
        {
            if (chunk.Blocks.NonEmptyBlocks <= 0)
                return;

            // Prepare a bounding box for our geometry
            int minX = chunk.MinBoundsC&0xFF;
            int minY = (chunk.MinBoundsC>>8)&0xFF;
            int minZ = (chunk.MinBoundsC>>16)&0xFF;
            int maxX = chunk.MaxBoundsC&0xFF;
            int maxY = (chunk.MaxBoundsC>>8)&0xFF;
            int maxZ = (chunk.MaxBoundsC>>16)&0xFF;
            Bounds bounds = new Bounds(
                new Vector3((minX+maxX)>>1, (minY+maxY)>>1, (minZ+maxZ)>>1),
                new Vector3(maxX-minX, maxY-minY, maxZ-minZ)
            );

            // Generate a mesh
            Batcher.Commit(
                chunk.world.transform.rotation*chunk.Pos+chunk.world.transform.position,
                chunk.world.transform.rotation,
                ref bounds
#if DEBUG
                , chunk.Pos+"C"
#endif
                );
        }
    }
}