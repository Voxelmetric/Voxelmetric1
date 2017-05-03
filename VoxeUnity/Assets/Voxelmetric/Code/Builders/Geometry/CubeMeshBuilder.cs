using System;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders.Geometry
{
    public class CubeMeshBuilder: IMeshBuilder
    {
        private static readonly int stepSize = 1;
        private static readonly int width = Env.ChunkSize;

        public void Build(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            ChunkBlocks blocks = chunk.blocks;
            ChunkStateManagerClient client = chunk.stateManager;
            var pools = chunk.pools;

            int[] mins = {minX, minY, minZ};
            int[] maxes = {maxX, maxY, maxZ};

            int[] x = {0, 0, 0}; // Relative position of a block
            int[] q = {0, 0, 0};
                // Direction in which we compare neighbors when building the mask (q[d] is our current direction)
            int[] du = {0, 0, 0}; // Width in a given dimension (du[u] is our current dimension)
            int[] dv = {0, 0, 0}; // Height in a given dimension (dv[v] is our current dimension)

            bool customBlockMaskInitialized = false;

            BlockFace[] mask = pools.BlockFaceArrayPool.PopExact(width*width);
            bool[] customBlockMask = pools.BoolArrayPool.PopExact(Env.ChunkSizeWithPaddingPow3);
            Vector3[] vecs = pools.Vector3ArrayPool.PopExact(4);

            int l, k, w, h, n;

            for (bool backFace = false, b = true; b!=backFace; backFace = true, b = !b)
            {
                // Iterate over 3 dimensions. Once for front faces, once for back faces
                for (uint d = 0; d<3; d++)
                {
                    uint u = d+1;
                    if (u>2) u = u-3; // u = (d+1)%3; <-- we know the range is within 1..3 so we can improvize
                    uint v = d+2;
                    if (v>2) v = v-3; // v = (d+2)%3; <-- we know the range is within 2..4 so we can improvize

                    x[0] = 0;
                    x[1] = 0;
                    x[2] = 0;

                    q[0] = 0;
                    q[1] = 0;
                    q[2] = 0;
                    q[d] = stepSize;

                    // Determine which side we're meshing
                    Direction dir = 0;
                    switch (d)
                    {
                        case 0:
                            dir = backFace ? Direction.west : Direction.east;
                            break;
                        case 1:
                            dir = backFace ? Direction.down : Direction.up;
                            break;
                        case 2:
                            dir = backFace ? Direction.south : Direction.north;
                            break;
                    }


                    // Move through the dimension from front to back
                    for (x[d] = mins[d]-1; x[d]<=maxes[d];)
                    {
                        // Compute the mask
                        n = 0;
                        Array.Clear(mask, 0, mask.Length);

                        for (x[v] = mins[v]; x[v]<=maxes[v]; x[v]++)
                        {
                            for (x[u] = mins[u]; x[u]<=maxes[u]; x[u]++, n++)
                            {
                                int realX = x[0];
                                int realY = x[1];
                                int realZ = x[2];

                                int index0 = Helpers.GetChunkIndex1DFrom3D(realX, realY, realZ);
                                int index1 = Helpers.GetChunkIndex1DFrom3D(realX+q[0], realY+q[1], realZ+q[2]);

                                Block voxelFace0 = blocks.GetBlock(index0);
                                Block voxelFace1 = blocks.GetBlock(index1);

                                if (backFace)
                                {
                                    // Let's see whether we can merge faces
                                    if (voxelFace1.CanBuildFaceWith(voxelFace0))
                                    {
                                        mask[n] = new BlockFace
                                        {
                                            block = voxelFace1,
                                            pos = new Vector3Int(realX + q[0], realY + q[1], realZ + q[2]),
                                            side = dir,
                                            light = voxelFace1.Custom ? new BlockLightData(0) : BlockUtils.CalculateColors(chunk, index1, dir),
                                            materialID = voxelFace1.RenderMaterialID
                                        };
                                    }
                                }
                                else
                                {
                                    // Let's see whether we can merge faces
                                    if (voxelFace0.CanBuildFaceWith(voxelFace1))
                                    {
                                        mask[n] = new BlockFace
                                        {
                                            block = voxelFace0,
                                            pos = new Vector3Int(realX, realY, realZ),
                                            side = dir,
                                            light = voxelFace0.Custom ? new BlockLightData(0) : BlockUtils.CalculateColors(chunk, index0, dir),
                                            materialID = voxelFace0.RenderMaterialID
                                        };
                                    }
                                }
                            }
                        }

                        x[d]++;
                        n = 0;

                        // Build faces from the mask if it's possible
                        int j;
                        for (j = 0; j<width; j++)
                        {
                            int i;
                            for (i = 0; i<width;)
                            {
                                if (mask[n].block==null)
                                {
                                    i++;
                                    n++;
                                    continue;
                                }

                                w = 1;
                                h = 1;
                                bool buildSingleFace = true;
                                BlockFace m = mask[n];
                                
                                // Custom blocks are treated differently. They are built whole at once instead of
                                // being build face by face. Therefore, we remember those we already processed and
                                // skip them the next time we come across them again
                                if (m.block.Custom)
                                {
                                    // Only clear the mask when necessary
                                    if (!customBlockMaskInitialized)
                                    {
                                        customBlockMaskInitialized = true;

                                        Array.Clear(customBlockMask, 0, Env.ChunkSizeWithPaddingPow3);
                                    }

                                    int index = Helpers.GetChunkIndex1DFrom3D(m.pos.x, m.pos.y, m.pos.z);
                                    if (!customBlockMask[index])
                                    {
                                        customBlockMask[index] = true;
                                        m.block.BuildBlock(chunk, ref m.pos, m.block.RenderMaterialID);
                                    }

                                    buildSingleFace = false;
                                }
                                // Don't render faces on world's edges for chunks with no neighbor
                                else if (Features.DontRenderWorldEdgesMask>0 && client.Listeners[(int)dir]==null)
                                {
                                    if ((Features.DontRenderWorldEdgesMask&Side.up)!=0 && dir==Direction.up && x[1]==Env.ChunkSize)
                                        buildSingleFace = false;
                                    else if ((Features.DontRenderWorldEdgesMask&Side.down)!=0 && dir==Direction.down && x[1]==0)
                                        buildSingleFace = false;
                                    else if ((Features.DontRenderWorldEdgesMask&Side.east)!=0 && dir==Direction.east && x[0]==Env.ChunkSize)
                                        buildSingleFace = false;
                                    else if ((Features.DontRenderWorldEdgesMask&Side.west)!=0 && dir==Direction.west && x[0]==0)
                                        buildSingleFace = false;
                                    else if ((Features.DontRenderWorldEdgesMask&Side.north)!=0 && dir==Direction.north && x[2]==Env.ChunkSize)
                                        buildSingleFace = false;
                                    else if ((Features.DontRenderWorldEdgesMask&Side.south)!=0 && dir==Direction.south && x[2]==0)
                                        buildSingleFace = false;
                                }

                                if (buildSingleFace)
                                {
                                    // Compute width
                                    int maskIndex = n + 1;
                                    for (w = 1; i + w < width;)
                                    {
                                        var blk = mask[maskIndex].block;
                                        if (blk == null ||
                                            !blk.CanMergeFaceWith(m.block) ||
                                            !mask[maskIndex].light.Equals(m.light))
                                            break;

                                        ++w;
                                        ++maskIndex;
                                    }

                                    // Compute height
                                    for (h = 1; j + h < width; h++)
                                    {
                                        maskIndex = n + h * width;
                                        for (k = 0; k < w; k++, maskIndex++)
                                        {
                                            var blk = mask[maskIndex].block;
                                            if (blk == null ||
                                                !blk.CanMergeFaceWith(m.block) ||
                                                !mask[maskIndex].light.Equals(m.light))
                                                goto cont;
                                        }
                                    }
                                    cont:
                                    // Prepare face coordinates and dimensions
                                    x[u] = i;
                                    x[v] = j;

                                    du[0] = du[1] = du[2] = 0;
                                    dv[0] = dv[1] = dv[2] = 0;
                                    du[u] = w;
                                    dv[v] = h;

                                    // Face vertices transformed to world coordinates
                                    // 0--1
                                    // |  |
                                    // |  |
                                    // 3--2
                                    if (d==2)
                                    {
                                        // Rotate north and south by 90 degrees counter clockwise
                                        vecs[3] = new Vector3(x[0], x[1], x[2])-BlockUtils.HalfBlockVector;
                                        vecs[0] = new Vector3(x[0]+du[0], x[1]+du[1], x[2]+du[2])-BlockUtils.HalfBlockVector;
                                        vecs[1] = new Vector3(x[0]+du[0]+dv[0], x[1]+du[1]+dv[1], x[2]+du[2]+dv[2])-BlockUtils.HalfBlockVector;
                                        vecs[2] = new Vector3(x[0]+dv[0], x[1]+dv[1], x[2]+dv[2])-BlockUtils.HalfBlockVector;
                                    }
                                    else
                                    {
                                        vecs[0] = new Vector3(x[0], x[1], x[2])-BlockUtils.HalfBlockVector;
                                        vecs[1] = new Vector3(x[0]+du[0], x[1]+du[1], x[2]+du[2])-BlockUtils.HalfBlockVector;
                                        vecs[2] = new Vector3(x[0]+du[0]+dv[0], x[1]+du[1]+dv[1], x[2]+du[2]+dv[2])-BlockUtils.HalfBlockVector;
                                        vecs[3] = new Vector3(x[0]+dv[0], x[1]+dv[1], x[2]+dv[2])-BlockUtils.HalfBlockVector;
                                    }

                                    m.block.BuildFace(chunk, vecs, ref m);
                                }

                                // Zero out the mask
                                for (l = 0; l<h; ++l)
                                {
                                    for (k = 0; k<w; ++k)
                                    {
                                        mask[n+k+l*width] = new BlockFace();
                                    }
                                }

                                i += w;
                                n += w;
                            }
                        }
                    }
                }
            }

            pools.BlockFaceArrayPool.Push(mask);
            pools.BoolArrayPool.Push(customBlockMask);
            pools.Vector3ArrayPool.Push(vecs);
        }
    }
}
