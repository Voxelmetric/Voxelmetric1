using System;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Blocks.Builders
{
    [Serializable]
    public static class BlockBuilder
    {
        public static void PrepareVertices(Chunk chunk, BlockPos localPos, BlockPos globalPos, VertexData[] vertexData, Direction direction)
        {
            PrepareFace(chunk, localPos, globalPos, vertexData, direction);
        }

        public static void PrepareColors(Chunk chunk, BlockPos localPos, BlockPos globalPos, VertexData[] vertexData, Direction direction)
        {
            bool nSolid = false;
            bool eSolid = false;
            bool sSolid = false;
            bool wSolid = false;

            bool wnSolid = false;
            bool neSolid = false;
            bool esSolid = false;
            bool swSolid = false;

            //float light = 0;

            ChunkBlocks blocks = chunk.blocks;
            Block block;

            switch (direction)
            {
                case Direction.up:
                    nSolid = blocks.LocalGet(localPos.Add(0, 1, 1)).IsSolid(Direction.south);
                    eSolid = blocks.LocalGet(localPos.Add(1, 1, 0)).IsSolid(Direction.west);
                    sSolid = blocks.LocalGet(localPos.Add(0, 1, -1)).IsSolid(Direction.north);
                    wSolid = blocks.LocalGet(localPos.Add(-1, 1, 0)).IsSolid(Direction.east);

                    block = blocks.LocalGet(localPos.Add(-1, 1, 1));
                    wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.south);
                    block = blocks.LocalGet(localPos.Add(1, 1, 1));
                    neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                    block = blocks.LocalGet(localPos.Add(1, 1, -1));
                    esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(-1, 1, -1));
                    swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);

                    //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, 1, 0)))/ 15f;
                    break;
                case Direction.down:
                    nSolid = blocks.LocalGet(localPos.Add(0, -1, -1)).IsSolid(Direction.south);
                    eSolid = blocks.LocalGet(localPos.Add(1, -1, 0)).IsSolid(Direction.west);
                    sSolid = blocks.LocalGet(localPos.Add(0, -1, 1)).IsSolid(Direction.north);
                    wSolid = blocks.LocalGet(localPos.Add(-1, -1, 0)).IsSolid(Direction.east);

                    block = blocks.LocalGet(localPos.Add(-1, -1, -1));
                    wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.south);
                    block = blocks.LocalGet(localPos.Add(1, -1, -1));
                    neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                    block = blocks.LocalGet(localPos.Add(1, -1, 1));
                    esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(-1, -1, 1));
                    swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);

                    //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, -1, 0))) / 15f;
                    break;
                case Direction.north:
                    nSolid = blocks.LocalGet(localPos.Add(1, 0, 1)).IsSolid(Direction.west);
                    eSolid = blocks.LocalGet(localPos.Add(0, 1, 1)).IsSolid(Direction.down);
                    sSolid = blocks.LocalGet(localPos.Add(-1, 0, 1)).IsSolid(Direction.east);
                    wSolid = blocks.LocalGet(localPos.Add(0, -1, 1)).IsSolid(Direction.up);

                    block = blocks.LocalGet(localPos.Add(-1, 1, 1));
                    esSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.south);
                    block = blocks.LocalGet(localPos.Add(1, 1, 1));
                    neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                    block = blocks.LocalGet(localPos.Add(1, -1, 1));
                    wnSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(-1, -1, 1));
                    swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);

                    //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, 0, 1))) / 15f;
                    break;
                case Direction.east:
                    nSolid = blocks.LocalGet(localPos.Add(1, 0, -1)).IsSolid(Direction.up);
                    eSolid = blocks.LocalGet(localPos.Add(1, 1, 0)).IsSolid(Direction.west);
                    sSolid = blocks.LocalGet(localPos.Add(1, 0, 1)).IsSolid(Direction.down);
                    wSolid = blocks.LocalGet(localPos.Add(1, -1, 0)).IsSolid(Direction.east);

                    block = blocks.LocalGet(localPos.Add(1, 1, 1));
                    esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(1, 1, -1));
                    neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                    block = blocks.LocalGet(localPos.Add(1, -1, -1));
                    wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(1, -1, 1));
                    swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);

                    //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(1, 0, 0))) / 15f;
                    break;
                case Direction.south:
                    nSolid = blocks.LocalGet(localPos.Add(-1, 0, -1)).IsSolid(Direction.down);
                    eSolid = blocks.LocalGet(localPos.Add(0, 1, -1)).IsSolid(Direction.west);
                    sSolid = blocks.LocalGet(localPos.Add(1, 0, -1)).IsSolid(Direction.up);
                    wSolid = blocks.LocalGet(localPos.Add(0, -1, -1)).IsSolid(Direction.south);

                    block = blocks.LocalGet(localPos.Add(1, 1, -1));
                    esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(-1, 1, -1));
                    neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                    block = blocks.LocalGet(localPos.Add(-1, -1, -1));
                    wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(1, -1, -1));
                    swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);

                    //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(0, 0, -1))) / 15f;
                    break;
                case Direction.west:
                    nSolid = blocks.LocalGet(localPos.Add(-1, 0, 1)).IsSolid(Direction.up);
                    eSolid = blocks.LocalGet(localPos.Add(-1, 1, 0)).IsSolid(Direction.west);
                    sSolid = blocks.LocalGet(localPos.Add(-1, 0, -1)).IsSolid(Direction.down);
                    wSolid = blocks.LocalGet(localPos.Add(-1, -1, 0)).IsSolid(Direction.east);

                    block = blocks.LocalGet(localPos.Add(-1, 1, -1));
                    esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(-1, 1, 1));
                    neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                    block = blocks.LocalGet(localPos.Add(-1, -1, 1));
                    wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.north);
                    block = blocks.LocalGet(localPos.Add(-1, -1, -1));
                    swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);

                    //light = BlockDataMap.NonSolid.Light(blocks.LocalGet(localPos.Add(-1, 0, 0))) / 15f;
                    break;
            }

            if (chunk.world.config.addAOToMesh)
            {
                SetColorsAO(vertexData, wnSolid, nSolid, neSolid, eSolid, esSolid, sSolid, swSolid, wSolid, chunk.world.config.ambientOcclusionStrength);
            }
            else
            {
                SetColors(vertexData, 1, 1, 1, 1, 1);
            }
        }

        public static void PrepareTexture(Chunk chunk, BlockPos localPos, BlockPos globalPos, VertexData[] vertexData, Direction direction, TextureCollection textureCollection)
        {
            Rect texture = textureCollection.GetTexture(chunk, localPos, globalPos, direction);

            vertexData[0].UV = new Vector2(texture.x + texture.width, texture.y);
            vertexData[1].UV = new Vector2(texture.x + texture.width, texture.y + texture.height);
            vertexData[2].UV = new Vector2(texture.x, texture.y + texture.height);
            vertexData[3].UV = new Vector2(texture.x, texture.y);
        }

        public static void PrepareTexture(Chunk chunk, BlockPos localPos, BlockPos globalPos, VertexData[] vertexData, Direction direction, TextureCollection[] textureCollections)
        {
            Rect texture = new Rect();

            switch (direction)
            {
                case Direction.up:
                    texture = textureCollections[0].GetTexture(chunk, localPos, globalPos, direction);
                    break;
                case Direction.down:
                    texture = textureCollections[1].GetTexture(chunk, localPos, globalPos, direction);
                    break;
                case Direction.north:
                    texture = textureCollections[2].GetTexture(chunk, localPos, globalPos, direction);
                    break;
                case Direction.east:
                    texture = textureCollections[3].GetTexture(chunk, localPos, globalPos, direction);
                    break;
                case Direction.south:
                    texture = textureCollections[4].GetTexture(chunk, localPos, globalPos, direction);
                    break;
                case Direction.west:
                    texture = textureCollections[5].GetTexture(chunk, localPos, globalPos, direction);
                    break;
            }
            
            vertexData[0].UV = new Vector2(texture.x + texture.width, texture.y);
            vertexData[1].UV = new Vector2(texture.x + texture.width, texture.y + texture.height);
            vertexData[2].UV = new Vector2(texture.x, texture.y + texture.height);
            vertexData[3].UV = new Vector2(texture.x, texture.y);
        }

        private static void PrepareFace(Chunk chunk, BlockPos localPos, BlockPos globalPos, VertexData[] vertexData, Direction direction)
        {
            //Adding a tiny overlap between block meshes may solve floating point imprecision
            //errors causing pixel size gaps between blocks when looking closely
            float halfBlock = (Env.BlockSize / 2) + Env.BlockFacePadding;

            //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
            Vector3 vPos = localPos;
            //Vector3 vPos = (pos - chunk.pos);

            switch (direction)
            {
                case Direction.up:
                    vertexData[0].Vertex = new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock);
                    vertexData[1].Vertex = new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock);
                    vertexData[2].Vertex = new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock);
                    vertexData[3].Vertex = new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock);
                    break;
                case Direction.down:
                    vertexData[0].Vertex = new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock);
                    vertexData[1].Vertex = new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock);
                    vertexData[2].Vertex = new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock);
                    vertexData[3].Vertex = new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock);
                    break;
                case Direction.north:
                    vertexData[0].Vertex = new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock);
                    vertexData[1].Vertex = new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock);
                    vertexData[2].Vertex = new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock);
                    vertexData[3].Vertex = new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock);
                    break;
                case Direction.east:
                    vertexData[0].Vertex = new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock);
                    vertexData[1].Vertex = new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock);
                    vertexData[2].Vertex = new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z + halfBlock);
                    vertexData[3].Vertex = new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z + halfBlock);
                    break;
                case Direction.south:
                    vertexData[0].Vertex = new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock);
                    vertexData[1].Vertex = new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock);
                    vertexData[2].Vertex = new Vector3(vPos.x + halfBlock, vPos.y + halfBlock, vPos.z - halfBlock);
                    vertexData[3].Vertex = new Vector3(vPos.x + halfBlock, vPos.y - halfBlock, vPos.z - halfBlock);
                    break;
                case Direction.west:
                    vertexData[0].Vertex = new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z + halfBlock);
                    vertexData[1].Vertex = new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z + halfBlock);
                    vertexData[2].Vertex = new Vector3(vPos.x - halfBlock, vPos.y + halfBlock, vPos.z - halfBlock);
                    vertexData[3].Vertex = new Vector3(vPos.x - halfBlock, vPos.y - halfBlock, vPos.z - halfBlock);
                    break;
                default:
                    Debug.LogError("Direction not recognized");
                    break;
            }

            chunk.render.batcher.AddFace(vertexData, DirectionUtils.Backface(direction));
        }

        private static void SetColorsAO(VertexData[] vertexData, bool wnSolid, bool nSolid, bool neSolid, bool eSolid, bool esSolid, bool sSolid, bool swSolid, bool wSolid, float strength)
        {
            float ne = 1;
            float es = 1;
            float sw = 1;
            float wn = 1;

            strength /= 2;

            if (nSolid)
            {
                wn -= strength;
                ne -= strength;
            }

            if (eSolid)
            {
                ne -= strength;
                es -= strength;
            }

            if (sSolid)
            {
                es -= strength;
                sw -= strength;
            }

            if (wSolid)
            {
                sw -= strength;
                wn -= strength;
            }

            if (neSolid)
                ne -= strength;

            if (swSolid)
                sw -= strength;

            if (wnSolid)
                wn -= strength;

            if (esSolid)
                es -= strength;

            SetColors(vertexData, ne, es, sw, wn, 1);
        }

        public static void SetColors(VertexData[] data, float ne, float es, float sw, float wn, float light)
        {
            wn = (wn * light);
            ne = (ne * light);
            es = (es * light);
            sw = (sw * light);

            data[0].Color = new Color(wn, wn, wn);
            data[1].Color = new Color(ne, ne, ne);
            data[2].Color = new Color(es, es, es);
            data[3].Color = new Color(sw, sw, sw);
        }

        public static void SetColors(VertexData[] data, ref Color color)
        {
            data[0].Color = color;
            data[1].Color = color;
            data[2].Color = color;
            data[3].Color = color;
        }
    }
}
