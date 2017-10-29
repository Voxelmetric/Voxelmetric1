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
            Globals.TerrainMeshBuilder.Build(chunk, out chunk.minBounds, out chunk.maxBounds);
        }

        public override void Commit()
        {
            if (chunk.blocks.NonEmptyBlocks<=0)
                return;

            // Prepare a bounding box for our geometry
            int minX = chunk.minBounds&0xFF;
            int minY = (chunk.minBounds>>8)&0xFF;
            int minZ = (chunk.minBounds>>16)&0xFF;
            int maxX = chunk.maxBounds&0xFF;
            int maxY = (chunk.maxBounds>>8)&0xFF;
            int maxZ = (chunk.maxBounds>>16)&0xFF;
            Bounds bounds = new Bounds(
                new Vector3((minX+maxX)>>1, (minY+maxY)>>1, (minZ+maxZ)>>1),
                new Vector3(maxX-minX, maxY-minY, maxZ-minZ)
            );

            // Generate a mesh
            Batcher.Commit(
                chunk.world.transform.rotation*chunk.pos+chunk.world.transform.position,
                chunk.world.transform.rotation,
                ref bounds
#if DEBUG
                , chunk.pos.ToString()
#endif
                );
        }
    }
}