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
            Globals.TerrainMeshBuilder.Build(chunk, out chunk.MinBounds, out chunk.NaxBounds);
        }

        public override void Commit()
        {
            if (chunk.Blocks.NonEmptyBlocks<=0)
                return;

            // Prepare a bounding box for our geometry
            int minX = chunk.MinBounds&0xFF;
            int minY = (chunk.MinBounds>>8)&0xFF;
            int minZ = (chunk.MinBounds>>16)&0xFF;
            int maxX = chunk.NaxBounds&0xFF;
            int maxY = (chunk.NaxBounds>>8)&0xFF;
            int maxZ = (chunk.NaxBounds>>16)&0xFF;
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
                , chunk.Pos.ToString()
#endif
                );
        }
    }
}