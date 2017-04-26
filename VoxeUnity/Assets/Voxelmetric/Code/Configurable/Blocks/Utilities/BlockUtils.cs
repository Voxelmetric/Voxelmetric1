using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

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
            }
        };

        public static void PrepareColors(Chunk chunk, VertexData[] vertexData, Direction direction, ref BlockLightData light)
        {
            if (chunk.world.config.addAOToMesh)
            {
                SetColorsAO(
                    vertexData,
                    light.wnSolid, light.nSolid, light.neSolid, light.eSolid,
                    light.esSolid, light.sSolid, light.swSolid, light.wSolid,
                    chunk.world.config.ambientOcclusionStrength, direction);
            }
            else
            {
                SetColors(vertexData, 1, 1, 1, 1, 1);
            }
        }

        public static BlockLightData CalculateColors(Chunk chunk, int localPosIndex, Direction direction)
        {
            // With AO turned off, do not generate any fancy data
            if (!chunk.world.config.addAOToMesh)
                return new BlockLightData(0);

            bool nSolid = false;
            bool eSolid = false;
            bool sSolid = false;
            bool wSolid = false;

            bool wnSolid = false;
            bool neSolid = false;
            bool esSolid = false;
            bool swSolid = false;

            ChunkBlocks blocks = chunk.blocks;
            int index, index2, index3;

            switch (direction)
            {
                case Direction.up:
                    index = localPosIndex + Env.ChunkSizeWithPaddingPow2; // + (0,1,0)
                    index2 = index - Env.ChunkSizeWithPadding;
                    index3 = index + Env.ChunkSizeWithPadding;

                    //swSolid = blocks.Get(localPos.Add(-1, 1, -1)).Solid;
                    swSolid = blocks.Get(index2 - 1).Solid;
                    //sSolid = blocks.Get(localPos.Add(0, 1, -1)).Solid;
                    sSolid = blocks.Get(index2).Solid;
                    //esSolid = blocks.Get(localPos.Add(1, 1, -1)).Solid;
                    esSolid = blocks.Get(index2 + 1).Solid;
                    //wSolid = blocks.Get(localPos.Add(-1, 1, 0)).Solid;
                    wSolid = blocks.Get(index - 1).Solid;
                    //eSolid = blocks.Get(localPos.Add(1, 1, 0)).Solid;
                    eSolid = blocks.Get(index + 1).Solid;
                    //wnSolid = blocks.Get(localPos.Add(-1, 1, 1)).Solid;
                    wnSolid = blocks.Get(index3 - 1).Solid;
                    //nSolid = blocks.Get(localPos.Add(0, 1, 1)).Solid;
                    nSolid = blocks.Get(index3).Solid;
                    //neSolid = blocks.Get(localPos.Add(1, 1, 1)).Solid;
                    neSolid = blocks.Get(index3 + 1).Solid;

                    break;
                case Direction.down:
                    index = localPosIndex - Env.ChunkSizeWithPaddingPow2; // - (0,1,0)
                    index2 = index - Env.ChunkSizeWithPadding;
                    index3 = index + Env.ChunkSizeWithPadding;

                    //wnSolid = blocks.Get(localPos.Add(-1, -1, -1)).Solid;
                    wnSolid = blocks.Get(index2 - 1).Solid;
                    //nSolid = blocks.Get(localPos.Add(0, -1, -1)).Solid;
                    nSolid = blocks.Get(index2).Solid;
                    //neSolid = blocks.Get(localPos.Add(1, -1, -1)).Solid;
                    neSolid = blocks.Get(index2 + 1).Solid;

                    //wSolid = blocks.Get(localPos.Add(-1, -1, 0)).Solid;
                    wSolid = blocks.Get(index - 1).Solid;
                    //eSolid = blocks.Get(localPos.Add(1, -1, 0)).Solid;
                    eSolid = blocks.Get(index + 1).Solid;

                    //swSolid = blocks.Get(localPos.Add(-1, -1, 1)).Solid;
                    swSolid = blocks.Get(index3 - 1).Solid;
                    //sSolid = blocks.Get(localPos.Add(0, -1, 1)).Solid;
                    sSolid = blocks.Get(index3).Solid;
                    //esSolid = blocks.Get(localPos.Add(1, -1, 1)).Solid;
                    esSolid = blocks.Get(index3 + 1).Solid;
                    break;
                case Direction.north:
                    index = localPosIndex + Env.ChunkSizeWithPadding; // + (0,0,1)
                    index2 = index - Env.ChunkSizeWithPaddingPow2;
                    index3 = index + Env.ChunkSizeWithPaddingPow2;

                    //swSolid = blocks.Get(localPos.Add(-1, -1, 1)).Solid;
                    swSolid = blocks.Get(index2 - 1).Solid;
                    //wSolid = blocks.Get(localPos.Add(0, -1, 1)).Solid;
                    wSolid = blocks.Get(index2).Solid;
                    //wnSolid = blocks.Get(localPos.Add(1, -1, 1)).Solid;
                    wnSolid = blocks.Get(index2 + 1).Solid;

                    //sSolid = blocks.Get(localPos.Add(-1, 0, 1)).Solid;
                    sSolid = blocks.Get(index - 1).Solid;
                    //nSolid = blocks.Get(localPos.Add(1, 0, 1)).Solid;
                    nSolid = blocks.Get(index + 1).Solid;

                    //esSolid = blocks.Get(localPos.Add(-1, 1, 1)).Solid;
                    esSolid = blocks.Get(index3 - 1).Solid;
                    //eSolid = blocks.Get(localPos.Add(0, 1, 1)).Solid;
                    eSolid = blocks.Get(index3).Solid;
                    //neSolid = blocks.Get(localPos.Add(1, 1, 1)).Solid;
                    neSolid = blocks.Get(index3 + 1).Solid;
                    break;
                case Direction.south:
                    index = localPosIndex - Env.ChunkSizeWithPadding; // - (0,0,1)
                    index2 = index - Env.ChunkSizeWithPaddingPow2;
                    index3 = index + Env.ChunkSizeWithPaddingPow2;

                    //wnSolid = blocks.Get(localPos.Add(-1, -1, -1)).Solid;
                    wnSolid = blocks.Get(index2 - 1).Solid;
                    //wSolid = blocks.Get(localPos.Add(0, -1, -1)).Solid;
                    wSolid = blocks.Get(index2).Solid;
                    //swSolid = blocks.Get(localPos.Add(1, -1, -1)).Solid;
                    swSolid = blocks.Get(index2 + 1).Solid;

                    //nSolid = blocks.Get(localPos.Add(-1, 0, -1)).Solid;
                    nSolid = blocks.Get(index - 1).Solid;
                    //sSolid = blocks.Get(localPos.Add(1, 0, -1)).Solid;
                    sSolid = blocks.Get(index + 1).Solid;

                    //neSolid = blocks.Get(localPos.Add(-1, 1, -1)).Solid;
                    neSolid = blocks.Get(index3 - 1).Solid;
                    //esSolid = blocks.Get(localPos.Add(1, 1, -1)).Solid;
                    esSolid = blocks.Get(index3).Solid;
                    //eSolid = blocks.Get(localPos.Add(0, 1, -1)).Solid;
                    eSolid = blocks.Get(index3 + 1).Solid;
                    break;
                case Direction.east:
                    index = localPosIndex + 1; // + (1,0,0)
                    index2 = index - Env.ChunkSizeWithPaddingPow2;
                    index3 = index + Env.ChunkSizeWithPaddingPow2;

                    //wnSolid = blocks.Get(localPos.Add(1, -1, -1)).Solid;
                    wnSolid = blocks.Get(index2 - Env.ChunkSizeWithPadding).Solid;
                    //wSolid = blocks.Get(localPos.Add(1, -1, 0)).Solid;
                    wSolid = blocks.Get(index2).Solid;
                    //swSolid = blocks.Get(localPos.Add(1, -1, 1)).Solid;
                    swSolid = blocks.Get(index2 + Env.ChunkSizeWithPadding).Solid;

                    //nSolid = blocks.Get(localPos.Add(1, 0, -1)).Solid;
                    nSolid = blocks.Get(index - Env.ChunkSizeWithPadding).Solid;
                    //sSolid = blocks.Get(localPos.Add(1, 0, 1)).Solid;
                    sSolid = blocks.Get(index + Env.ChunkSizeWithPadding).Solid;

                    //neSolid = blocks.Get(localPos.Add(1, 1, -1)).Solid;
                    neSolid = blocks.Get(index3 - Env.ChunkSizeWithPadding).Solid;
                    //eSolid = blocks.Get(localPos.Add(1, 1, 0)).Solid;
                    eSolid = blocks.Get(index3).Solid;
                    //esSolid = blocks.Get(localPos.Add(1, 1, 1)).Solid;
                    esSolid = blocks.Get(index3 + Env.ChunkSizeWithPadding).Solid;
                    break;
                case Direction.west:
                    index = localPosIndex - 1; // - (1,0,0)
                    index2 = index - Env.ChunkSizeWithPaddingPow2;
                    index3 = index + Env.ChunkSizeWithPaddingPow2;

                    //swSolid = blocks.Get(localPos.Add(-1, -1, -1)).Solid;
                    swSolid = blocks.Get(index2 - Env.ChunkSizeWithPadding).Solid;
                    //wSolid = blocks.Get(localPos.Add(-1, -1, 0)).Solid;
                    wSolid = blocks.Get(index2).Solid;
                    //wnSolid = blocks.Get(localPos.Add(-1, -1, 1)).Solid;
                    wnSolid = blocks.Get(index2 + Env.ChunkSizeWithPadding).Solid;

                    //sSolid = blocks.Get(localPos.Add(-1, 0, -1)).Solid;
                    sSolid = blocks.Get(index - Env.ChunkSizeWithPadding).Solid;
                    //nSolid = blocks.Get(localPos.Add(-1, 0, 1)).Solid;
                    nSolid = blocks.Get(index + Env.ChunkSizeWithPadding).Solid;

                    //esSolid = blocks.Get(localPos.Add(-1, 1, -1)).Solid;
                    esSolid = blocks.Get(index3 - Env.ChunkSizeWithPadding).Solid;
                    //eSolid = blocks.Get(localPos.Add(-1, 1, 0)).Solid;
                    eSolid = blocks.Get(index3).Solid;
                    //neSolid = blocks.Get(localPos.Add(-1, 1, 1)).Solid;
                    neSolid = blocks.Get(index3 + Env.ChunkSizeWithPadding).Solid;
                    break;
            }

            return new BlockLightData(wnSolid, nSolid, neSolid, eSolid, esSolid, sSolid, swSolid, wSolid);
        }
        
        public static void AdjustColors(Chunk chunk, VertexData[] vertexData, Direction direction, BlockLightData light)
        {
            if (!chunk.world.config.addAOToMesh)
                return;

            AdjustColorsAO(vertexData,
                light.wnSolid, light.nSolid, light.neSolid, light.eSolid,
                light.esSolid, light.sSolid, light.swSolid, light.wSolid,
                chunk.world.config.ambientOcclusionStrength, direction);
        }

        public static void PrepareTexture(Chunk chunk, ref Vector3Int localPos, VertexData[] vertexData, Direction direction, TextureCollection textureCollection)
        {
            Rect texture = textureCollection.GetTexture(chunk, ref localPos, direction);

            vertexData[3].UV = new Vector2(texture.x+texture.width, texture.y);
            vertexData[2].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
            vertexData[1].UV = new Vector2(texture.x, texture.y+texture.height);
            vertexData[0].UV = new Vector2(texture.x, texture.y);
        }

        public static void PrepareTexture(Chunk chunk, ref Vector3Int localPos, VertexData[] vertexData, Direction direction, TextureCollection[] textureCollections)
        {
            Rect texture = textureCollections[(int)direction].GetTexture(chunk, ref localPos, direction);

            vertexData[3].UV = new Vector2(texture.x+texture.width, texture.y);
            vertexData[2].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
            vertexData[1].UV = new Vector2(texture.x, texture.y+texture.height);
            vertexData[0].UV = new Vector2(texture.x, texture.y);
        }

        public static void PrepareVertices(ref Vector3Int localPos, VertexData[] vertexData, Direction direction)
        {
            Vector3 vPos = localPos;
            int d = DirectionUtils.Get(direction);

            vertexData[0].Vertex = vPos+HalfBlockOffsets[d][0];
            vertexData[1].Vertex = vPos+HalfBlockOffsets[d][1];
            vertexData[2].Vertex = vPos+HalfBlockOffsets[d][2];
            vertexData[3].Vertex = vPos+HalfBlockOffsets[d][3];
        }

        private static void SetColorsAO(VertexData[] vertexData, bool wnSolid, bool nSolid, bool neSolid, bool eSolid, bool esSolid, bool sSolid, bool swSolid, bool wSolid, float strength, Direction direction)
        {
            float ne = 1f;
            float es = 1f;
            float sw = 1f;
            float wn = 1f;

            strength *= 0.5f;

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

            SetColors(vertexData, ne, es, sw, wn, 1, direction);
        }

        private static void AdjustColorsAO(VertexData[] vertexData, bool wnSolid, bool nSolid, bool neSolid, bool eSolid, bool esSolid, bool sSolid, bool swSolid, bool wSolid, float strength, Direction direction)
        {
            float ne = 1f;
            float es = 1f;
            float sw = 1f;
            float wn = 1f;

            strength *= 0.5f;

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

            AdjustColors(vertexData, ne, es, sw, wn, 1, direction);
        }

        public static void SetColors(VertexData[] data, float ne, float es, float sw, float wn, float light, Direction direction=Direction.up)
        {
            byte wn_ = (byte)(wn*light*255.0f);
            byte ne_ = (byte)(ne*light*255.0f);
            byte es_ = (byte)(es*light*255.0f);
            byte sw_ = (byte)(sw*light*255.0f);

            switch (direction)
            {
                case Direction.down:
                    data[0].Color = new Color32(wn_, wn_, wn_, 255);
                    data[3].Color = new Color32(ne_, ne_, ne_, 255);
                    data[2].Color = new Color32(es_, es_, es_, 255);
                    data[1].Color = new Color32(sw_, sw_, sw_, 255);
                    break;
                case Direction.up:
                    data[1].Color = new Color32(wn_, wn_, wn_, 255);
                    data[2].Color = new Color32(ne_, ne_, ne_, 255);
                    data[3].Color = new Color32(es_, es_, es_, 255);
                    data[0].Color = new Color32(sw_, sw_, sw_, 255);
                    break;
                case Direction.north:
                case Direction.east:
                    data[0].Color = new Color32(wn_, wn_, wn_, 255);
                    data[1].Color = new Color32(ne_, ne_, ne_, 255);
                    data[2].Color = new Color32(es_, es_, es_, 255);
                    data[3].Color = new Color32(sw_, sw_, sw_, 255);
                    break;
                default: // east, south
                    data[3].Color = new Color32(wn_, wn_, wn_, 255);
                    data[2].Color = new Color32(ne_, ne_, ne_, 255);
                    data[1].Color = new Color32(es_, es_, es_, 255);
                    data[0].Color = new Color32(sw_, sw_, sw_, 255);
                    break;
            }
        }

        private static Color32 ToColor32(ref Color32 col, float coef)
        {
            return new Color32(
                (byte)((float)col.r*coef*100.0f/100.0f),
                (byte)((float)col.g*coef*100.0f/100.0f),
                (byte)((float)col.b*coef*100.0f/100.0f),
                col.a
                );
        }
        public static void AdjustColors(VertexData[] data, float ne, float es, float sw, float wn, float light, Direction direction = Direction.up)
        {
            wn = (wn * light);
            ne = (ne * light);
            es = (es * light);
            sw = (sw * light);

            switch (direction)
            {
                case Direction.down:
                    data[0].Color = ToColor32(ref data[0].Color, wn);
                    data[3].Color = ToColor32(ref data[3].Color, ne);
                    data[2].Color = ToColor32(ref data[2].Color, es);
                    data[1].Color = ToColor32(ref data[1].Color, sw);
                    break;
                case Direction.up:
                    data[1].Color = ToColor32(ref data[1].Color, wn);
                    data[2].Color = ToColor32(ref data[2].Color, ne);
                    data[3].Color = ToColor32(ref data[3].Color, es);
                    data[0].Color = ToColor32(ref data[0].Color, sw);
                    break;
                case Direction.north:
                case Direction.east:
                    data[0].Color = ToColor32(ref data[0].Color, wn);
                    data[1].Color = ToColor32(ref data[1].Color, ne);
                    data[2].Color = ToColor32(ref data[2].Color, es);
                    data[3].Color = ToColor32(ref data[3].Color, sw);
                    break;
                default: // east, south
                    data[3].Color = ToColor32(ref data[3].Color, wn);
                    data[2].Color = ToColor32(ref data[2].Color, ne);
                    data[1].Color = ToColor32(ref data[1].Color, es);
                    data[0].Color = ToColor32(ref data[0].Color, sw);
                    break;
            }
        }
    }
}