using UnityEngine;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Rendering.GeometryBatcher;

namespace Voxelmetric.Code.Configurable.Blocks.Utilities
{
    public static class MeshUtils {

        public static void BuildCrossMesh(Chunk chunk, Vector3Int localPos, TextureCollection texture, bool useOffset, int materialID)
        {
            LocalPools pools = chunk.pools;
            RenderGeometryBatcher batcher = chunk.GeometryHandler.Batcher;

            float halfBlock = (Env.BlockSize / 2) + Env.BlockFacePadding;

            float blockHeight = 1;
            float offsetX = 0;
            float offsetZ = 0;

            //Using the block positions hash is much better for random numbers than saving the offset and height in the block data
            if (useOffset)
            {
                int hash = localPos.GetHashCode();
                if (hash < 0)
                    hash *= -1;

                blockHeight = halfBlock * 2 * (hash % 100) / 100f;

                hash *= 39;
                if (hash < 0)
                    hash *= -1;

                offsetX = (halfBlock * (hash % 100) / 100f) - (halfBlock / 2);

                hash *= 39;
                if (hash < 0)
                    hash *= -1;

                offsetZ = (halfBlock * (hash % 100) / 100f) - (halfBlock / 2);
            }

            //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
            Vector3 vPos = localPos;
            vPos += new Vector3(offsetX, 0, offsetZ);
            
            VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
            {
                vertexData[0].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                BlockUtils.PrepareTexture(chunk, ref localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);
                batcher.AddFace(vertexData, false, materialID);
            }
            {
                vertexData[0].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                BlockUtils.PrepareTexture(chunk, ref localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);
                batcher.AddFace(vertexData, false, materialID);
            }
            {
                vertexData[0].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                BlockUtils.PrepareTexture(chunk, ref localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);
                batcher.AddFace(vertexData, false, materialID);
            }
            {
                vertexData[0].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                BlockUtils.PrepareTexture(chunk, ref localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);
                batcher.AddFace(vertexData, false, materialID);
            }
            pools.VertexDataArrayPool.Push(vertexData);
        }
    }
}
