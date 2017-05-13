using UnityEngine;
using Voxelmetric.Code.Common;
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
                SetColorsAO(vertexData, light, chunk.world.config.ambientOcclusionStrength);
            else
                SetColors(vertexData, 1f, 1f, 1f, 1f, false, 1f);
        }

        public static BlockLightData CalculateColors(Chunk chunk, int localPosIndex, Direction direction)
        {
            // With AO turned off, do not generate any fancy data
            if (!chunk.world.config.addAOToMesh)
                return new BlockLightData(0);

            // Side blocks
            bool nSolid, eSolid, sSolid, wSolid;
            // Corner blocks
            bool nwSolid, neSolid, seSolid, swSolid;

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
                default://case Direction.west:
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

            AdjustColorsAO(vertexData, light, chunk.world.config.ambientOcclusionStrength);
        }

        public static void PrepareTexture(Chunk chunk, ref Vector3Int localPos, VertexData[] vertexData, Direction direction, TextureCollection textureCollection, bool rotated)
        {
            Rect texture = textureCollection.GetTexture(chunk, ref localPos, direction);

            if (!rotated)
            {
                vertexData[0].UV = new Vector2(texture.x, texture.y);
                vertexData[1].UV = new Vector2(texture.x, texture.y+texture.height);
                vertexData[2].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
                vertexData[3].UV = new Vector2(texture.x+texture.width, texture.y);
            }
            else
            {
                vertexData[0].UV = new Vector2(texture.x, texture.y+texture.height);
                vertexData[1].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
                vertexData[2].UV = new Vector2(texture.x+texture.width, texture.y);
                vertexData[3].UV = new Vector2(texture.x, texture.y);
            }
        }

        public static void PrepareTexture(Chunk chunk, ref Vector3Int localPos, VertexData[] vertexData, Direction direction, TextureCollection[] textureCollections, bool rotated)
        {
            Rect texture = textureCollections[(int)direction].GetTexture(chunk, ref localPos, direction);

            if (!rotated)
            {
                vertexData[0].UV = new Vector2(texture.x, texture.y);
                vertexData[1].UV = new Vector2(texture.x, texture.y+texture.height);
                vertexData[2].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
                vertexData[3].UV = new Vector2(texture.x+texture.width, texture.y);
            }
            else
            {
                vertexData[0].UV = new Vector2(texture.x, texture.y+texture.height);
                vertexData[1].UV = new Vector2(texture.x+texture.width, texture.y+texture.height);
                vertexData[2].UV = new Vector2(texture.x+texture.width, texture.y);
                vertexData[3].UV = new Vector2(texture.x, texture.y);
            }
        }

        private static void SetColorsAO(VertexData[] vertexData, BlockLightData light, float strength)
        {
            float ne = 1f-(light.neAO * 0.25f) * strength;
            float se = 1f-(light.seAO * 0.25f) * strength;
            float sw = 1f-(light.swAO * 0.25f) * strength;
            float nw = 1f-(light.nwAO * 0.25f) * strength;

            SetColors(vertexData, sw, nw, ne, se, light.FaceRotationNecessary, 1f);
        }

        private static void AdjustColorsAO(VertexData[] vertexData, BlockLightData light, float strength)
        {
            float ne = 1f-(light.neAO * 0.25f) * strength;
            float se = 1f-(light.seAO * 0.25f) * strength;
            float sw = 1f-(light.swAO * 0.25f) * strength;
            float nw = 1f-(light.nwAO * 0.25f) * strength;

            AdjustColors(vertexData, sw, nw, ne, se, light.FaceRotationNecessary, 1f);
        }

        public static void SetColors(VertexData[] data, float sw, float nw, float ne, float se, bool rotated, float light)
        {
            float _sw = (sw*light*255.0f).Clamp(0f,255f);
            float _nw = (nw*light*255.0f).Clamp(0f,255f);
            float _ne = (ne*light*255.0f).Clamp(0f,255f);
            float _se = (se*light*255.0f).Clamp(0f,255f);

            byte sw_ = (byte)_sw;
            byte nw_ = (byte)_nw;
            byte ne_ = (byte)_ne;
            byte se_ = (byte)_se;

            if (!rotated)
            {
                data[0].Color = new Color32(sw_, sw_, sw_, 255);
                data[1].Color = new Color32(nw_, nw_, nw_, 255);
                data[2].Color = new Color32(ne_, ne_, ne_, 255);
                data[3].Color = new Color32(se_, se_, se_, 255);
            }
            else
            {
                data[0].Color = new Color32(nw_, nw_, nw_, 255);
                data[1].Color = new Color32(ne_, ne_, ne_, 255);
                data[2].Color = new Color32(se_, se_, se_, 255);
                data[3].Color = new Color32(sw_, sw_, sw_, 255);
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

        public static void AdjustColors(VertexData[] data, float sw, float nw, float ne, float se, bool rotated, float light)
        {
            sw = (sw*light).Clamp(0f,1f);
            nw = (nw*light).Clamp(0f,1f);
            ne = (ne*light).Clamp(0f,1f);
            se = (se*light).Clamp(0f,1f);

            if (!rotated)
            {
                data[0].Color = ToColor32(ref data[0].Color, sw);
                data[1].Color = ToColor32(ref data[1].Color, nw);
                data[2].Color = ToColor32(ref data[2].Color, ne);
                data[3].Color = ToColor32(ref data[3].Color, se);
            }
            else
            {
                data[0].Color = ToColor32(ref data[0].Color, nw);
                data[1].Color = ToColor32(ref data[1].Color, ne);
                data[2].Color = ToColor32(ref data[2].Color, se);
                data[3].Color = ToColor32(ref data[3].Color, sw);
            }
        }
    }
}