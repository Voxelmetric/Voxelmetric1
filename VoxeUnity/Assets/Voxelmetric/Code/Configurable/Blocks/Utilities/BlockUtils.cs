using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Configurable.Blocks.Utilities
{
    public static class BlockUtils
    {
        //Adding a tiny overlap between block meshes may solve floating point imprecision
        //errors causing pixel size gaps between blocks when looking closely
        public static readonly float halfBlock = (Env.BlockSize/2)+Env.BlockFacePadding;

        public static readonly Vector3 HalfBlockVector = new Vector3(halfBlock, halfBlock, halfBlock);

        public static readonly Vector3[][] HalfBlockOffsets =
        {
            new[]
            {
                // Direction.north
                new Vector3(+halfBlock, -halfBlock, +halfBlock),
                new Vector3(+halfBlock, +halfBlock, +halfBlock),
                new Vector3(-halfBlock, +halfBlock, +halfBlock),
                new Vector3(-halfBlock, -halfBlock, +halfBlock)
            },
            new[]
            {
                // Direction.south
                new Vector3(-halfBlock, -halfBlock, -halfBlock),
                new Vector3(-halfBlock, +halfBlock, -halfBlock),
                new Vector3(+halfBlock, +halfBlock, -halfBlock),
                new Vector3(+halfBlock, -halfBlock, -halfBlock),
            },

            new[]
            {
                // Direction.east
                new Vector3(+halfBlock, -halfBlock, -halfBlock),
                new Vector3(+halfBlock, +halfBlock, -halfBlock),
                new Vector3(+halfBlock, +halfBlock, +halfBlock),
                new Vector3(+halfBlock, -halfBlock, +halfBlock)
            },
            new[]
            {
                // Direction.west
                new Vector3(-halfBlock, -halfBlock, +halfBlock),
                new Vector3(-halfBlock, +halfBlock, +halfBlock),
                new Vector3(-halfBlock, +halfBlock, -halfBlock),
                new Vector3(-halfBlock, -halfBlock, -halfBlock),
            },

            new[]
            {
                // Direction.up
                new Vector3(-halfBlock, +halfBlock, +halfBlock),
                new Vector3(+halfBlock, +halfBlock, +halfBlock),
                new Vector3(+halfBlock, +halfBlock, -halfBlock),
                new Vector3(-halfBlock, +halfBlock, -halfBlock)
            },
            new[]
            {
                // Direction.down
                new Vector3(-halfBlock, -halfBlock, -halfBlock),
                new Vector3(+halfBlock, -halfBlock, -halfBlock),
                new Vector3(+halfBlock, -halfBlock, +halfBlock),
                new Vector3(-halfBlock, -halfBlock, +halfBlock),
            },
        };

        public static void PrepareColors(Chunk chunk, Vector3Int localPos, VertexData[] vertexData, Direction direction)
        {
            if (chunk.world.config.addAOToMesh)
            {
                bool nSolid = false;
                bool eSolid = false;
                bool sSolid = false;
                bool wSolid = false;

                bool wnSolid = false;
                bool neSolid = false;
                bool esSolid = false;
                bool swSolid = false;

                ChunkBlocks blocks = chunk.blocks;
                Block block;

                switch (direction)
                {
                    case Direction.up:
                        nSolid = blocks.GetBlock(localPos.Add(0, 1, 1)).IsSolid(Direction.south);
                        eSolid = blocks.GetBlock(localPos.Add(1, 1, 0)).IsSolid(Direction.west);
                        sSolid = blocks.GetBlock(localPos.Add(0, 1, -1)).IsSolid(Direction.north);
                        wSolid = blocks.GetBlock(localPos.Add(-1, 1, 0)).IsSolid(Direction.east);

                        block = blocks.GetBlock(localPos.Add(-1, 1, 1));
                        wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.south);
                        block = blocks.GetBlock(localPos.Add(1, 1, 1));
                        neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                        block = blocks.GetBlock(localPos.Add(1, 1, -1));
                        esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(-1, 1, -1));
                        swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);
                        break;
                    case Direction.down:
                        nSolid = blocks.GetBlock(localPos.Add(0, -1, -1)).IsSolid(Direction.south);
                        eSolid = blocks.GetBlock(localPos.Add(1, -1, 0)).IsSolid(Direction.west);
                        sSolid = blocks.GetBlock(localPos.Add(0, -1, 1)).IsSolid(Direction.north);
                        wSolid = blocks.GetBlock(localPos.Add(-1, -1, 0)).IsSolid(Direction.east);

                        block = blocks.GetBlock(localPos.Add(-1, -1, -1));
                        wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.south);
                        block = blocks.GetBlock(localPos.Add(1, -1, -1));
                        neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                        block = blocks.GetBlock(localPos.Add(1, -1, 1));
                        esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(-1, -1, 1));
                        swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);
                        break;
                    case Direction.north:
                        nSolid = blocks.GetBlock(localPos.Add(1, 0, 1)).IsSolid(Direction.west);
                        eSolid = blocks.GetBlock(localPos.Add(0, 1, 1)).IsSolid(Direction.down);
                        sSolid = blocks.GetBlock(localPos.Add(-1, 0, 1)).IsSolid(Direction.east);
                        wSolid = blocks.GetBlock(localPos.Add(0, -1, 1)).IsSolid(Direction.up);

                        block = blocks.GetBlock(localPos.Add(-1, 1, 1));
                        esSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.south);
                        block = blocks.GetBlock(localPos.Add(1, 1, 1));
                        neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                        block = blocks.GetBlock(localPos.Add(1, -1, 1));
                        wnSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(-1, -1, 1));
                        swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);
                        break;
                    case Direction.east:
                        nSolid = blocks.GetBlock(localPos.Add(1, 0, -1)).IsSolid(Direction.up);
                        eSolid = blocks.GetBlock(localPos.Add(1, 1, 0)).IsSolid(Direction.west);
                        sSolid = blocks.GetBlock(localPos.Add(1, 0, 1)).IsSolid(Direction.down);
                        wSolid = blocks.GetBlock(localPos.Add(1, -1, 0)).IsSolid(Direction.east);

                        block = blocks.GetBlock(localPos.Add(1, 1, 1));
                        esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(1, 1, -1));
                        neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                        block = blocks.GetBlock(localPos.Add(1, -1, -1));
                        wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(1, -1, 1));
                        swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);
                        break;
                    case Direction.south:
                        nSolid = blocks.GetBlock(localPos.Add(-1, 0, -1)).IsSolid(Direction.down);
                        eSolid = blocks.GetBlock(localPos.Add(0, 1, -1)).IsSolid(Direction.west);
                        sSolid = blocks.GetBlock(localPos.Add(1, 0, -1)).IsSolid(Direction.up);
                        wSolid = blocks.GetBlock(localPos.Add(0, -1, -1)).IsSolid(Direction.south);

                        block = blocks.GetBlock(localPos.Add(1, 1, -1));
                        esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(-1, 1, -1));
                        neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                        block = blocks.GetBlock(localPos.Add(-1, -1, -1));
                        wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(1, -1, -1));
                        swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);
                        break;
                    case Direction.west:
                        nSolid = blocks.GetBlock(localPos.Add(-1, 0, 1)).IsSolid(Direction.up);
                        eSolid = blocks.GetBlock(localPos.Add(-1, 1, 0)).IsSolid(Direction.west);
                        sSolid = blocks.GetBlock(localPos.Add(-1, 0, -1)).IsSolid(Direction.down);
                        wSolid = blocks.GetBlock(localPos.Add(-1, -1, 0)).IsSolid(Direction.east);

                        block = blocks.GetBlock(localPos.Add(-1, 1, -1));
                        esSolid = block.IsSolid(Direction.west) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(-1, 1, 1));
                        neSolid = block.IsSolid(Direction.south) && block.IsSolid(Direction.west);
                        block = blocks.GetBlock(localPos.Add(-1, -1, 1));
                        wnSolid = block.IsSolid(Direction.east) && block.IsSolid(Direction.north);
                        block = blocks.GetBlock(localPos.Add(-1, -1, -1));
                        swSolid = block.IsSolid(Direction.north) && block.IsSolid(Direction.east);
                        break;
                }

                SetColorsAO(vertexData, wnSolid, nSolid, neSolid, eSolid, esSolid, sSolid, swSolid, wSolid,
                            chunk.world.config.ambientOcclusionStrength);
            }
            else
            {
                SetColors(vertexData, 1, 1, 1, 1, 1);
            }
        }

        public static void PrepareTexture(Chunk chunk, Vector3Int localPos, VertexData[] vertexData, Direction direction, TextureCollection textureCollection)
        {
            Rect texture = textureCollection.GetTexture(chunk, localPos, direction);

            vertexData[0].UV = new Vector2(texture.x+texture.width, texture.y);
            vertexData[1].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
            vertexData[2].UV = new Vector2(texture.x, texture.y+texture.height);
            vertexData[3].UV = new Vector2(texture.x, texture.y);
        }

        public static void PrepareTexture(Chunk chunk, Vector3Int localPos, VertexData[] vertexData, Direction direction, TextureCollection[] textureCollections)
        {
            Rect texture = new Rect();

            switch (direction)
            {
                case Direction.up:
                    texture = textureCollections[0].GetTexture(chunk, localPos, direction);
                    break;
                case Direction.down:
                    texture = textureCollections[1].GetTexture(chunk, localPos, direction);
                    break;
                case Direction.north:
                    texture = textureCollections[2].GetTexture(chunk, localPos, direction);
                    break;
                case Direction.east:
                    texture = textureCollections[3].GetTexture(chunk, localPos, direction);
                    break;
                case Direction.south:
                    texture = textureCollections[4].GetTexture(chunk, localPos, direction);
                    break;
                case Direction.west:
                    texture = textureCollections[5].GetTexture(chunk, localPos, direction);
                    break;
            }

            vertexData[0].UV = new Vector2(texture.x+texture.width, texture.y);
            vertexData[1].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
            vertexData[2].UV = new Vector2(texture.x, texture.y+texture.height);
            vertexData[3].UV = new Vector2(texture.x, texture.y);
        }

        public static void PrepareVertices(Vector3Int localPos, VertexData[] vertexData, Direction direction)
        {
            //Converting the position to a vector adjusts it based on block size and gives us real world coordinates for x, y and z
            Vector3 vPos = localPos;
            //Vector3 vPos = (pos - chunk.pos);

            int d = DirectionUtils.Get(direction);
            vertexData[0].Vertex = vPos+HalfBlockOffsets[d][0];
            vertexData[1].Vertex = vPos+HalfBlockOffsets[d][1];
            vertexData[2].Vertex = vPos+HalfBlockOffsets[d][2];
            vertexData[3].Vertex = vPos+HalfBlockOffsets[d][3];
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