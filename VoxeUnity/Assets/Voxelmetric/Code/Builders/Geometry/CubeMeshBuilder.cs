using System;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders.Geometry
{
    public class CubeMeshBuilder: IMeshBuilder
    {
        private static readonly int stepSize = 1;
        private static readonly int width = Env.ChunkSize;

        public void Build(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            World world = chunk.world;
            ChunkBlocks blocks = chunk.blocks;

            bool renderingRestricted = world.IsWorldCoordsRestricted() && !world.config.renderBottomWorldFaces && chunk.pos.y==chunk.world.config.minY;

            int[] mins = {minX, minY, minZ};
            int[] maxes = {maxX, maxY, maxZ};

            int[] x = {0, 0, 0}; // Relative position of a block
            int[] q = {0, 0, 0}; // Direction in which we compare neighbors when building the mask (q[d] is our current direction)
            int[] du = {0, 0, 0}; // Width in a given dimension (du[u] is our current dimension)
            int[] dv = {0, 0, 0}; // Height in a given dimension (dv[v] is our current dimension)

            BlockFace[] mask = chunk.pools.BlockFaceArrayPool.Pop(width * width);
            bool[] customBlockMask = chunk.pools.BoolArrayPool.Pop(Env.ChunkSizeWithPaddingPow3);
            bool customBlockMaskInitialized = false;
            Vector3[] vecs = chunk.pools.Vector3ArrayPool.Pop(4);

            for (bool backFace = false, b = true; b!=backFace; backFace = true, b = !b)
            {
                // Iterate over 3 dimensions. Once for front faces, once for back faces
                for (uint d = 0; d<3; d++)
                {
                    uint u = d + 1;
                    if (u > 2) u = u - 3; // u = (d+1)%3; <-- we know the range is within 1..3 so we can improvize
                    uint v = d + 2;
                    if (v > 2) v = v - 3; // v = (d+2)%3; <-- we know the range is within 2..4 so we can improvize

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

                    // Do not create faces facing downwards for blocks at the bottom of the world
                    bool ignoreBottomFace = renderingRestricted && d==1 && backFace;

                    // Move through the dimension from front to back
                    for (x[d] = mins[d]-1; x[d]<=maxes[d];)
                    {
                        // Compute the mask
                        int n = 0;

                        for (x[v] = 0; x[v]<mins[v]; x[v]++)
                        {
                            for (x[u] = 0; x[u]<width; x[u]++)
                                mask[n++] = new BlockFace();
                        }

                        for (x[v] = mins[v]; x[v]<=maxes[v]; x[v]++)
                        {
                            for (x[u] = 0; x[u]<mins[u]; x[u]++)
                                mask[n++] = new BlockFace();

                            for (x[u] = mins[u]; x[u]<=maxes[u]; x[u]++)
                            {
                                int realX = x[0];
                                int realY = x[1];
                                int realZ = x[2];

                                int index0 = Helpers.GetChunkIndex1DFrom3D(realX, realY, realZ);
                                int index1 = Helpers.GetChunkIndex1DFrom3D(realX+q[0], realY+q[1], realZ+q[2]);

                                Block voxelFace0 = blocks.GetBlock(index0);
                                Block voxelFace1 = blocks.GetBlock(index1);

                                mask[n++] = backFace
                                                ? (voxelFace1.CanBuildFaceWith(voxelFace0)
                                                       ? new BlockFace
                                                       {
                                                           block = voxelFace1,
                                                           pos = new Vector3Int(realX+q[0], realY+q[1], realZ+q[2]),
                                                           side = dir
                                                       }
                                                       : new BlockFace())
                                                : (voxelFace0.CanBuildFaceWith(voxelFace1)
                                                       ? new BlockFace
                                                       {
                                                           block = voxelFace0,
                                                           pos = new Vector3Int(realX, realY, realZ),
                                                           side = dir
                                                       }
                                                       : new BlockFace());
                            }

                            for (x[u] = maxes[u]+1; x[u]<width; x[u]++)
                                mask[n++] = new BlockFace();
                        }

                        for (x[v] = maxes[v]+1; x[v]<width; x[v]++)
                        {
                            for (x[u] = 0; x[u]<width; x[u]++)
                                mask[n++] = new BlockFace();
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

                                // Compute width
                                int w = 1;
                                // Compute height
                                int h = 1;

                                BlockFace m = mask[n];
                                if (m.block.Custom)
                                {
                                    // Only clear the mask when necessary
                                    if (!customBlockMaskInitialized)
                                    {
                                        customBlockMaskInitialized = true;

                                        Array.Clear(customBlockMask, 0, Env.ChunkSizeWithPaddingPow3);
                                    }

                                    // Custom blocks are treated differently. They are build whole at once instead of
                                    // being build face by face. Therefore, we remember those we processed and skip
                                    // them next time
                                    int index = Helpers.GetChunkIndex1DFrom3D(m.pos.x, m.pos.y, m.pos.z);
                                    if (customBlockMask[index]==false)
                                    {
                                        customBlockMask[index] = true;
                                        m.block.BuildBlock(chunk, m.pos);
                                    }
                                }
                                else if (ignoreBottomFace && x[1]==0)
                                {
                                    // Skip bottom faces at the bottom of the world
                                }
                                else
                                {
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

                                    m.block.BuildFace(chunk, m.pos, vecs, m.side);
                                }

                                // Zero out the mask
                                int l;
                                for (l = 0; l<h; ++l)
                                {
                                    int k;
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

            chunk.pools.BlockFaceArrayPool.Push(mask);
            chunk.pools.BoolArrayPool.Push(customBlockMask);
            chunk.pools.Vector3ArrayPool.Push(vecs);
        }
    }
}
