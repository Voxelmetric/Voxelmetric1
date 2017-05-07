using System;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders.Geometry
{
    /// <summary>
    /// Generates a cubical mesh with merged faces
    /// </summary>
    public class CubeMeshBuilder: MergedFacesMeshBuilder
    {
        protected override void BuildBox(Chunk chunk, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            // All faces in the are build in the following order:
            //     1--2
            //     |  |
            //     |  |
            //     0--3

            var blocks = chunk.blocks;
            var pools = chunk.pools;
            var listeners = chunk.stateManager.Listeners;
            Block block = blocks.GetBlock(Helpers.GetChunkIndex1DFrom3D(minX, minY, minZ));

            // Custom blocks have their own rules
            if (block.Custom)
            {
                for (int yy = minY; yy<maxY; yy++)
                {
                    for (int zz = minZ; zz<maxZ; zz++)
                    {
                        for (int xx = minX; xx<maxX; xx++)
                        {
                            Vector3Int pos = new Vector3Int(xx, yy, zz);
                            block.BuildBlock(chunk, ref pos, block.RenderMaterialID);
                        }
                    }
                }

                return;
            }

            int n, w, h, l, k, maskIndex;
            Vector3Int texturePos = new Vector3Int(minX, minY, minZ);

            Vector3[] face = pools.Vector3ArrayPool.PopExact(4);
            BlockFace[] mask = pools.BlockFaceArrayPool.PopExact(sideSize*sideSize);

            // Top
            if (listeners[(int)Direction.up]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                (Features.DontRenderWorldEdgesMask&Side.up)==0 ||
                maxY!=Env.ChunkSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // z axis - height

                // Build the mask
                for (int zz = minZ; zz<maxZ; ++zz)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n)
                    {
                        int currentIndex = Helpers.GetChunkIndex1DFrom3D(xx, maxY-1, zz);
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, maxY, zz);
                        Block neighborBlock = blocks.GetBlock(neighborIndex);

                        // Let's see whether we can merge faces
                        if (block.CanBuildFaceWith(neighborBlock))
                        {
                            mask[n] = new BlockFace
                            {
                                block = block,
                                pos = texturePos,
                                side = Direction.up,
                                light = BlockUtils.CalculateColors(chunk, currentIndex, Direction.up),
                                materialID = block.RenderMaterialID
                            };
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int zz = minZ; zz<maxZ; ++zz)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n].block==null)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        // Compute width
                        maskIndex = n+1;
                        for (w = 1; xx+w<sideSize;)
                        {
                            var blk = mask[maskIndex].block;
                            if (blk==null ||
                                blk.Type!=mask[n].block.Type ||
                                !mask[maskIndex].light.Equals(mask[n].light))
                                break;

                            ++w;
                            ++maskIndex;
                        }

                        // Compute height
                        for (h = 1; zz+h<sideSize; h++)
                        {
                            maskIndex = n+h*sideSize;
                            for (k = 0; k<w; k++, maskIndex++)
                            {
                                var blk = mask[maskIndex].block;
                                if (blk==null ||
                                    blk.Type!=mask[n].block.Type ||
                                    !mask[maskIndex].light.Equals(mask[n].light))
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        face[0] = new Vector3(xx, maxY, zz)+
                                  new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[1] = new Vector3(xx, maxY, zz+h)+
                                  new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[2] = new Vector3(xx+w, maxY, zz+h)+
                                  new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[3] = new Vector3(xx+w, maxY, zz)+
                                  new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);

                        block.BuildFace(chunk, face, ref mask[n]);

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = new BlockFace();
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            // Bottom
            if (listeners[(int)Direction.down]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                (Features.DontRenderWorldEdgesMask&Side.down)==0 ||
                minY!=0)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // z axis - height

                // Build the mask
                for (int zz = minZ; zz<maxZ; ++zz)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n)
                    {
                        int currentIndex = Helpers.GetChunkIndex1DFrom3D(xx, minY, zz);
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, minY-1, zz);
                        Block neighborBlock = blocks.GetBlock(neighborIndex);

                        // Let's see whether we can merge faces
                        if (block.CanBuildFaceWith(neighborBlock))
                        {
                            mask[n] = new BlockFace
                            {
                                block = block,
                                pos = texturePos,
                                side = Direction.down,
                                light = BlockUtils.CalculateColors(chunk, currentIndex, Direction.down),
                                materialID = block.RenderMaterialID
                            };
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int zz = minZ; zz<maxZ; ++zz)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n].block==null)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        // Compute width
                        maskIndex = n+1;
                        for (w = 1; xx+w<sideSize;)
                        {
                            var blk = mask[maskIndex].block;
                            if (blk==null ||
                                blk.Type!=mask[n].block.Type ||
                                !mask[maskIndex].light.Equals(mask[n].light))
                                break;

                            ++w;
                            ++maskIndex;
                        }

                        // Compute height
                        for (h = 1; zz+h<sideSize; h++)
                        {
                            maskIndex = n+h*sideSize;
                            for (k = 0; k<w; k++, maskIndex++)
                            {
                                var blk = mask[maskIndex].block;
                                if (blk==null ||
                                    blk.Type!=mask[n].block.Type ||
                                    !mask[maskIndex].light.Equals(mask[n].light))
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        face[0] = new Vector3(xx, minY, zz) +
                                  new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[1] = new Vector3(xx, minY, zz + h) +
                                  new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[2] = new Vector3(xx + w, minY, zz + h) +
                                  new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[3] = new Vector3(xx + w, minY, zz) +
                                  new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);

                        block.BuildFace(chunk, face, ref mask[n]);

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = new BlockFace();
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            // Right
            if (listeners[(int)Direction.east]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                (Features.DontRenderWorldEdgesMask&Side.east)==0 ||
                maxX!=Env.ChunkSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // y axis - height
                // z axis - width

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ; ++zz, ++n)
                    {
                        int currentIndex = Helpers.GetChunkIndex1DFrom3D(maxX-1, yy, zz);
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(maxX, yy, zz);
                        Block neighborBlock = blocks.GetBlock(neighborIndex);

                        // Let's see whether we can merge faces
                        if (block.CanBuildFaceWith(neighborBlock))
                        {
                            mask[n] = new BlockFace
                            {
                                block = block,
                                pos = texturePos,
                                side = Direction.east,
                                light = BlockUtils.CalculateColors(chunk, currentIndex, Direction.east),
                                materialID = block.RenderMaterialID
                            };
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ;)
                    {
                        if (mask[n].block==null)
                        {
                            ++zz;
                            ++n;
                            continue;
                        }

                        // Compute width
                        maskIndex = n+1;
                        for (w = 1; zz+w<sideSize;)
                        {
                            var blk = mask[maskIndex].block;
                            if (blk==null ||
                                blk.Type!=mask[n].block.Type ||
                                !mask[maskIndex].light.Equals(mask[n].light))
                                break;

                            ++w;
                            ++maskIndex;
                        }

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            maskIndex = n+h*sideSize;
                            for (k = 0; k<w; k++, maskIndex++)
                            {
                                var blk = mask[maskIndex].block;
                                if (blk==null ||
                                    blk.Type!=mask[n].block.Type ||
                                    !mask[maskIndex].light.Equals(mask[n].light))
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        face[0] = new Vector3(maxX, yy, zz)
                            + new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[1] = new Vector3(maxX, yy+h, zz)
                            + new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[2] = new Vector3(maxX, yy+h, zz+w)
                            + new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[3] = new Vector3(maxX, yy, zz+w)
                            + new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);

                        block.BuildFace(chunk, face, ref mask[n]);

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = new BlockFace();
                        }

                        zz += w;
                        n += w;
                    }
                }
            }
            // Left
            if (listeners[(int)Direction.west]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                (Features.DontRenderWorldEdgesMask&Side.west)==0 ||
                minX!=0)
            {
                Array.Clear(mask, 0, mask.Length);

                // y axis - height
                // z axis - width

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ; ++zz, ++n)
                    {
                        int currentIndex = Helpers.GetChunkIndex1DFrom3D(minX, yy, zz);
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX-1, yy, zz);
                        Block neighborBlock = blocks.GetBlock(neighborIndex);

                        // Let's see whether we can merge faces
                        if (block.CanBuildFaceWith(neighborBlock))
                        {
                            mask[n] = new BlockFace
                            {
                                block = block,
                                pos = texturePos,
                                side = Direction.west,
                                light = BlockUtils.CalculateColors(chunk, currentIndex, Direction.west),
                                materialID = block.RenderMaterialID
                            };
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ;)
                    {
                        if (mask[n].block==null)
                        {
                            ++zz;
                            ++n;
                            continue;
                        }

                        // Compute width
                        maskIndex = n+1;
                        for (w = 1; zz+w<sideSize;)
                        {
                            var blk = mask[maskIndex].block;
                            if (blk==null ||
                                blk.Type!=mask[n].block.Type ||
                                !mask[maskIndex].light.Equals(mask[n].light))
                                break;

                            ++w;
                            ++maskIndex;
                        }

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            maskIndex = n+h*sideSize;
                            for (k = 0; k<w; k++, maskIndex++)
                            {
                                var blk = mask[maskIndex].block;
                                if (blk==null ||
                                    blk.Type!=mask[n].block.Type ||
                                    !mask[maskIndex].light.Equals(mask[n].light))
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        face[0] = new Vector3(minX, yy, zz)
                            + new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[1] = new Vector3(minX, yy + h, zz)
                            + new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[2] = new Vector3(minX, yy + h, zz + w)
                            + new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[3] = new Vector3(minX, yy, zz + w)
                            + new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);

                        block.BuildFace(chunk, face, ref mask[n]);

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = new BlockFace();
                        }

                        zz += w;
                        n += w;
                    }
                }
            }
            // Front
            if (listeners[(int)Direction.north]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                (Features.DontRenderWorldEdgesMask&Side.north)==0 ||
                maxZ!=Env.ChunkSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // y axis - height

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n)
                    {
                        int currentIndex = Helpers.GetChunkIndex1DFrom3D(xx, yy, maxZ-1);
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, yy, maxZ);
                        Block neighborBlock = blocks.GetBlock(neighborIndex);

                        // Let's see whether we can merge faces
                        if (block.CanBuildFaceWith(neighborBlock))
                        {
                            mask[n] = new BlockFace
                            {
                                block = block,
                                pos = texturePos,
                                side = Direction.north,
                                light = BlockUtils.CalculateColors(chunk, currentIndex, Direction.north),
                                materialID = block.RenderMaterialID
                            };
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n].block==null)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        // Compute width
                        maskIndex = n+1;
                        for (w = 1; xx+w<sideSize;)
                        {
                            var blk = mask[maskIndex].block;
                            if (blk==null ||
                                blk.Type!=mask[n].block.Type ||
                                !mask[maskIndex].light.Equals(mask[n].light))
                                break;

                            ++w;
                            ++maskIndex;
                        }

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            maskIndex = n+h*sideSize;
                            for (k = 0; k<w; k++, maskIndex++)
                            {
                                var blk = mask[maskIndex].block;
                                if (blk==null ||
                                    blk.Type!=mask[n].block.Type ||
                                    !mask[maskIndex].light.Equals(mask[n].light))
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        face[0] = new Vector3(xx, yy, maxZ)
                            + new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[1] = new Vector3(xx, yy+h, maxZ)
                            + new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[2] = new Vector3(xx+w, yy+h, maxZ)
                            + new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                        face[3] = new Vector3(xx+w, yy, maxZ)
                            + new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);

                        block.BuildFace(chunk, face, ref mask[n]);

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = new BlockFace();
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            // Back
            if (listeners[(int)Direction.south]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                (Features.DontRenderWorldEdgesMask&Side.south)==0 ||
                minZ!=0)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // y axis - height

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n)
                    {
                        int currentIndex = Helpers.GetChunkIndex1DFrom3D(xx, yy, minZ);
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, yy, minZ-1);
                        Block neighborBlock = blocks.GetBlock(neighborIndex);

                        // Let's see whether we can merge faces
                        if (block.CanBuildFaceWith(neighborBlock))
                        {
                            mask[n] = new BlockFace
                            {
                                block = block,
                                pos = texturePos,
                                side = Direction.south,
                                light = BlockUtils.CalculateColors(chunk, currentIndex, Direction.south),
                                materialID = block.RenderMaterialID
                            };
                        }
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n].block==null)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        // Compute width
                        maskIndex = n+1;
                        for (w = 1; xx+w<sideSize;)
                        {
                            var blk = mask[maskIndex].block;
                            if (blk==null ||
                                blk.Type!=mask[n].block.Type ||
                                !mask[maskIndex].light.Equals(mask[n].light))
                                break;

                            ++w;
                            ++maskIndex;
                        }

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            maskIndex = n+h*sideSize;
                            for (k = 0; k<w; k++, maskIndex++)
                            {
                                var blk = mask[maskIndex].block;
                                if (blk==null ||
                                    blk.Type!=mask[n].block.Type ||
                                    !mask[maskIndex].light.Equals(mask[n].light))
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        face[0] = new Vector3(xx, yy, minZ)
                            + new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[1] = new Vector3(xx, yy+h, minZ)
                            + new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[2] = new Vector3(xx+w, yy+h, minZ)
                            + new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                        face[3] = new Vector3(xx+w, yy, minZ)
                            + new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);

                        block.BuildFace(chunk, face, ref mask[n]);

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = new BlockFace();
                        }

                        xx += w;
                        n += w;
                    }
                }
            }

            pools.BlockFaceArrayPool.Push(mask);
            pools.Vector3ArrayPool.Push(face);
        }
    }
}
