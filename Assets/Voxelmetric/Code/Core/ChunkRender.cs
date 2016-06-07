using UnityEngine;
using Voxelmetric.Code.Core.Interface;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public class ChunkRender: IChunkRender
    {
        protected MeshData meshData = new MeshData();
        protected Chunk chunk;

        public Mesh mesh
        {
            get { return meshData.mesh; }
        }

        public ChunkRender(Chunk chunk)
        {
            this.chunk = chunk;
        }

        public void Reset()
        {
        }

        /// <summary> Updates the chunk based on its contents </summary>
        public void BuildMeshData()
        {
            foreach (BlockPos localBlockPos in new BlockPosEnumerable(Env.ChunkSizePos))
            {
                Block block = chunk.blocks.LocalGet(localBlockPos);
                if (block.type==0)
                    continue;

                block.BuildBlock(chunk, localBlockPos, localBlockPos+chunk.pos, meshData);
            }

            meshData.ConvertToArrays();
        }

        public void BuildMesh()
        {
            meshData.CommitMesh();
        }
    }
}