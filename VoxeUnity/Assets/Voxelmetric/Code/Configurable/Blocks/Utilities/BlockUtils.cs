using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Configurable.Blocks.Utilities
{
    public static class BlockUtils
    {
        /// All faces in the engine are build in the following order:
        ///     1--2
        ///     |  |
        ///     |  |
        ///     0--3

        //Adding a tiny overlap between block meshes may solve floating point imprecision
        //errors causing pixel size gaps between blocks when looking closely
        public static readonly float blockPadding = Env.BlockFacePadding;
        
        public static readonly Vector3[][] PaddingOffsets =
        {
            new[]
            {
                // Direction.up
                new Vector3(-blockPadding, +blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, +blockPadding, -blockPadding)
            },
            new[]
            {
                // Direction.down
                new Vector3(-blockPadding, -blockPadding, -blockPadding),
                new Vector3(-blockPadding, -blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, -blockPadding),
            },

            new[]
            {
                // Direction.north
                new Vector3(-blockPadding, -blockPadding, +blockPadding),
                new Vector3(-blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, +blockPadding)
            },
            new[]
            {
                // Direction.south
                new Vector3(-blockPadding, -blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, -blockPadding),
                new Vector3(+blockPadding, +blockPadding, -blockPadding),
                new Vector3(+blockPadding, -blockPadding, -blockPadding),
            },

            new[]
            {
                // Direction.east
                new Vector3(+blockPadding, -blockPadding, -blockPadding),
                new Vector3(+blockPadding, +blockPadding, -blockPadding),
                new Vector3(+blockPadding, +blockPadding, +blockPadding),
                new Vector3(+blockPadding, -blockPadding, +blockPadding)
            },
            new[]
            {
                // Direction.west
                new Vector3(-blockPadding, -blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, -blockPadding),
                new Vector3(-blockPadding, +blockPadding, +blockPadding),
                new Vector3(-blockPadding, -blockPadding, +blockPadding),
            }
        };

        public static void PrepareColors(Chunk chunk, VertexData[] vertexData, Direction direction, ref BlockLightData light)
        {
            if (chunk.world.config.addAOToMesh)
            {
                SetColorsAO(
                    vertexData,
                    light.nwSolid, light.nSolid, light.neSolid, light.eSolid,
                    light.seSolid, light.sSolid, light.swSolid, light.wSolid,
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

            bool nwSolid = false;
            bool neSolid = false;
            bool seSolid = false;
            bool swSolid = false;

            ChunkBlocks blocks = chunk.blocks;
            int index, index2, index3;

            switch (direction)
            {
                case Direction.up:
                    index = localPosIndex + Env.ChunkSizeWithPaddingPow2; // + (0,1,0)
                    index2 = index - Env.ChunkSizeWithPadding; // - (0,0,1)
                    index3 = index + Env.ChunkSizeWithPadding; // + (0,0,1)
                    
                    swSolid = blocks.Get(index2 - 1).Solid; // -1,1,-1
                    sSolid = blocks.Get(index2).Solid;      //  0,1,-1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,1,-1
                    wSolid = blocks.Get(index - 1).Solid;   // -1,1, 0
                    eSolid = blocks.Get(index + 1).Solid;   //  1,1, 0
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1,1, 1
                    nSolid = blocks.Get(index3).Solid;      //  0,1, 1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1,1, 1
                    break;
                case Direction.down:
                    index = localPosIndex - Env.ChunkSizeWithPaddingPow2; // - (0,1,0)
                    index2 = index - Env.ChunkSizeWithPadding; // - (0,0,1)
                    index3 = index + Env.ChunkSizeWithPadding; // + (0,0,1)

                    swSolid = blocks.Get(index2 - 1).Solid; // -1,-1,-1
                    sSolid = blocks.Get(index2).Solid;      //  0,-1,-1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,-1,-1
                    wSolid = blocks.Get(index - 1).Solid;   // -1,-1, 0
                    eSolid = blocks.Get(index + 1).Solid;   //  1,-1, 0
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1,-1, 1
                    nSolid = blocks.Get(index3).Solid;      //  0,-1, 1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1,-1, 1
                    break;
                case Direction.north:
                    index = localPosIndex + Env.ChunkSizeWithPadding; // + (0,0,1)
                    index2 = index - Env.ChunkSizeWithPaddingPow2; // - (0,1,0)
                    index3 = index + Env.ChunkSizeWithPaddingPow2; // + (0,1,0)

                    swSolid = blocks.Get(index2 - 1).Solid; // -1,-1,1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,-1,1
                    wSolid = blocks.Get(index - 1).Solid;   // -1, 0,1
                    eSolid = blocks.Get(index + 1).Solid;   //  1, 0,1
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1, 1,1
                    sSolid = blocks.Get(index2).Solid;      //  0,-1,1
                    nSolid = blocks.Get(index3).Solid;      //  0, 1,1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1, 1,1
                    break;
                case Direction.south:
                    index = localPosIndex - Env.ChunkSizeWithPadding; // - (0,0,1)
                    index2 = index - Env.ChunkSizeWithPaddingPow2; // - (0,1,0)
                    index3 = index + Env.ChunkSizeWithPaddingPow2; // + (0,1,0)
                    
                    swSolid = blocks.Get(index2 - 1).Solid; // -1,-1,-1
                    seSolid = blocks.Get(index2 + 1).Solid; //  1,-1,-1
                    wSolid = blocks.Get(index - 1).Solid;   // -1, 0,-1
                    eSolid = blocks.Get(index + 1).Solid;   //  1, 0,-1
                    nwSolid = blocks.Get(index3 - 1).Solid; // -1, 1,-1
                    sSolid = blocks.Get(index2).Solid;      //  0,-1,-1
                    nSolid = blocks.Get(index3).Solid;      //  0, 1,-1
                    neSolid = blocks.Get(index3 + 1).Solid; //  1, 1,-1
                    break;
                case Direction.east:
                    index = localPosIndex+1; // + (1,0,0)
                    index2 = index - Env.ChunkSizeWithPaddingPow2; // - (0,1,0)
                    index3 = index + Env.ChunkSizeWithPaddingPow2; // + (0,1,0)
                    
                    swSolid = blocks.Get(index2 - Env.ChunkSizeWithPadding).Solid;  // 1,-1,-1
                    sSolid = blocks.Get(index2).Solid;                              // 1,-1, 0
                    seSolid = blocks.Get(index2 + Env.ChunkSizeWithPadding).Solid;  // 1,-1, 1
                    wSolid = blocks.Get(index - Env.ChunkSizeWithPadding).Solid;    // 1, 0,-1
                    eSolid = blocks.Get(index + Env.ChunkSizeWithPadding).Solid;    // 1, 0, 1
                    nwSolid = blocks.Get(index3 - Env.ChunkSizeWithPadding).Solid;  // 1, 1,-1
                    nSolid = blocks.Get(index3).Solid;                              // 1, 1, 0
                    neSolid = blocks.Get(index3 + Env.ChunkSizeWithPadding).Solid;  // 1, 1, 1
                    break;
                case Direction.west:
                    index = localPosIndex-1; // - (1,0,0)
                    index2 = index - Env.ChunkSizeWithPaddingPow2; // - (0,1,0)
                    index3 = index + Env.ChunkSizeWithPaddingPow2; // + (0,1,0)

                    swSolid = blocks.Get(index2 - Env.ChunkSizeWithPadding).Solid;  // -1,-1,-1
                    sSolid = blocks.Get(index2).Solid;                              // -1,-1, 0
                    seSolid = blocks.Get(index2 + Env.ChunkSizeWithPadding).Solid;  // -1,-1, 1
                    wSolid = blocks.Get(index - Env.ChunkSizeWithPadding).Solid;    // -1, 0,-1
                    eSolid = blocks.Get(index + Env.ChunkSizeWithPadding).Solid;    // -1, 0, 1
                    nwSolid = blocks.Get(index3 - Env.ChunkSizeWithPadding).Solid;  // -1, 1,-1
                    nSolid = blocks.Get(index3).Solid;                              // -1, 1, 0
                    neSolid = blocks.Get(index3 + Env.ChunkSizeWithPadding).Solid;  // -1, 1, 1
                    break;
            }

            return new BlockLightData(nwSolid, nSolid, neSolid, eSolid, seSolid, sSolid, swSolid, wSolid);
        }
        
        public static void AdjustColors(Chunk chunk, VertexData[] vertexData, Direction direction, BlockLightData light)
        {
            if (!chunk.world.config.addAOToMesh)
                return;

            AdjustColorsAO(vertexData,
                light.nwSolid, light.nSolid, light.neSolid, light.eSolid,
                light.seSolid, light.sSolid, light.swSolid, light.wSolid,
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
        
        private static void SetColorsAO(VertexData[] vertexData, bool nwSolid, bool nSolid, bool neSolid, bool eSolid, bool seSolid, bool sSolid, bool swSolid, bool wSolid, float strength, Direction direction)
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

            if (nwSolid)
                wn -= strength;

            if (seSolid)
                es -= strength;

            SetColors(vertexData, sw, wn, ne, es, 1, direction);
        }

        private static void AdjustColorsAO(VertexData[] vertexData, bool nwSolid, bool nSolid, bool neSolid, bool eSolid, bool seSolid, bool sSolid, bool swSolid, bool wSolid, float strength, Direction direction)
        {
            float ne = 1f;
            float nw = 1f;
            float se = 1f;
            float sw = 1f;

            strength *= 0.5f;

            if (nSolid)
            {
                nw -= strength;
                ne -= strength;
            }

            if (eSolid)
            {
                ne -= strength;
                se -= strength;
            }

            if (sSolid)
            {
                se -= strength;
                sw -= strength;
            }

            if (wSolid)
            {
                sw -= strength;
                nw -= strength;
            }

            if (neSolid)
                ne -= strength;

            if (swSolid)
                sw -= strength;

            if (nwSolid)
                nw -= strength;

            if (seSolid)
                se -= strength;

            AdjustColors(vertexData, sw, nw, ne, se, 1, direction);
        }

        public static void SetColors(VertexData[] data, float sw, float nw, float ne, float se, float light, Direction direction=Direction.up)
        {
            byte sw_ = (byte)(sw*light*255.0f);
            byte nw_ = (byte)(nw*light*255.0f);
            byte ne_ = (byte)(ne*light*255.0f);
            byte se_ = (byte)(se*light*255.0f);

            data[0].Color = new Color32(sw_, sw_, sw_, 255);
            data[1].Color = new Color32(nw_, nw_, nw_, 255);
            data[2].Color = new Color32(ne_, ne_, ne_, 255);
            data[3].Color = new Color32(se_, se_, se_, 255);
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
        public static void AdjustColors(VertexData[] data, float sw, float nw, float ne, float se, float light, Direction direction = Direction.up)
        {
            sw = (sw * light);
            nw = (nw * light);
            ne = (ne * light);
            se = (se * light);

            data[0].Color = ToColor32(ref data[0].Color, sw);
            data[1].Color = ToColor32(ref data[1].Color, nw);
            data[2].Color = ToColor32(ref data[2].Color, ne);
            data[3].Color = ToColor32(ref data[3].Color, se);
        }
    }
}