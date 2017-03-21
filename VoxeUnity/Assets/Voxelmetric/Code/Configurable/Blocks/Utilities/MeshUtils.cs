using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Configurable.Blocks.Utilities
{
    public static class MeshUtils {

        public static void BuildCrossMesh(Chunk chunk, Vector3Int localPos, TextureCollection texture, bool useOffset = true)
        {
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

            VertexData[] vertexData = chunk.pools.VertexDataArrayPool.PopExact(4);
            for (int i = 0; i < 4; i++)
                vertexData[i] = chunk.pools.VertexDataPool.Pop();
            VertexDataFixed[] vertexDataFixed = chunk.pools.VertexDataFixedArrayPool.PopExact(4);
            {
                vertexData[0].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                BlockUtils.PrepareTexture(chunk, localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);

                for (int i = 0; i<4; i++)
                    vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
                chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, false);
            }
            {
                vertexData[0].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                BlockUtils.PrepareTexture(chunk, localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);

                for (int i = 0; i<4; i++)
                    vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
                chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, false);
            }
            {
                vertexData[0].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                BlockUtils.PrepareTexture(chunk, localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);

                for (int i = 0; i<4; i++)
                    vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
                chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, false);
            }
            {
                vertexData[0].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock, vPos.z-halfBlock);
                vertexData[1].Vertex = new Vector3(vPos.x-halfBlock, vPos.y-halfBlock+blockHeight, vPos.z-halfBlock);
                vertexData[2].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock+blockHeight, vPos.z+halfBlock);
                vertexData[3].Vertex = new Vector3(vPos.x+halfBlock, vPos.y-halfBlock, vPos.z+halfBlock);
                BlockUtils.PrepareTexture(chunk, localPos, vertexData, Direction.north, texture);
                BlockUtils.SetColors(vertexData, 1, 1, 1, 1, 1);

                for (int i = 0; i<4; i++)
                    vertexDataFixed[i] = VertexDataUtils.ClassToStruct(vertexData[i]);
                chunk.GeometryHandler.Batcher.AddFace(vertexDataFixed, false);
            }
            chunk.pools.VertexDataFixedArrayPool.Push(vertexDataFixed);
            for (int i = 0; i < 4; i++)
                chunk.pools.VertexDataPool.Push(vertexData[i]);
            chunk.pools.VertexDataArrayPool.Push(vertexData);
        }
    }
}
