using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Builders.Collider
{
    /// <summary>
    /// Generates a typical cubical voxel geometry for a chunk
    /// </summary>
    public class CubeMeshColliderBuilder: IMeshBuilder
    {
        private static readonly int stepSize = 1;
        private static readonly int width = Env.ChunkSize;

        public void Build(Chunk chunk, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
        {
            World world = chunk.world;
            ChunkBlocks blocks = chunk.blocks;
            var pools = chunk.pools;

            bool colliderRestricted = world.IsWorldCoordsRestricted() && chunk.pos.y==chunk.world.config.minY;

            int[] mins = {minX, minY, minZ};
            int[] maxes = {maxX, maxY, maxZ};

            int[] x = {0, 0, 0}; // Relative position of a block
            int[] q = {0, 0, 0};
            // Direction in which we compare neighbors when building mask (q[d] is our current direction)
            int[] du = {0, 0, 0}; // Width in a given dimension (du[u] is our current dimension)
            int[] dv = {0, 0, 0}; // Height in a given dimension (dv[v] is our current dimension)

            bool[] mask = pools.BoolArrayPool.Pop(width*width);
            Vector3[] vecs = pools.Vector3ArrayPool.Pop(4);

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

                    /*// Determine which side we're meshing
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
                    }*/

                    // Do not create faces facing downwards for blocks at the bottom of the world
                    bool ignoreBottomFace = colliderRestricted && d==1 && backFace;

                    // Move through the dimension from front to back
                    for (x[d] = mins[d]-1; x[d]<=maxes[d];)
                    {
                        // Compute the mask
                        int n = 0;

                        for (x[v] = 0; x[v]<mins[v]; x[v]++)
                        {
                            for (x[u] = 0; x[u]<width; x[u]++)
                                mask[n++] = false;
                        }

                        for (x[v] = mins[v]; x[v]<=maxes[v]; x[v]++)
                        {
                            for (x[u] = 0; x[u]<mins[u]; x[u]++)
                                mask[n++] = false;

                            for (x[u] = mins[u]; x[u]<=maxes[u]; x[u]++)
                            {
                                int realX = x[0];
                                int realY = x[1];
                                int realZ = x[2];

                                bool voxelFace0 = blocks.GetBlock(new Vector3Int(realX, realY, realZ)).canBeWalkedOn;
                                bool voxelFace1 =
                                    blocks.GetBlock(new Vector3Int(realX+q[0], realY+q[1], realZ+q[2])).canBeWalkedOn;

                                mask[n++] = (!voxelFace0 || !voxelFace1) && (backFace ? voxelFace1 : voxelFace0);
                            }

                            for (x[u] = maxes[u]+1; x[u]<width; x[u]++)
                                mask[n++] = false;
                        }

                        for (x[v] = maxes[v]+1; x[v]<width; x[v]++)
                        {
                            for (x[u] = 0; x[u]<width; x[u]++)
                                mask[n++] = false;
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
                                if (mask[n]==false)
                                {
                                    i++;
                                    n++;
                                    continue;
                                }

                                bool m = mask[n];

                                // Compute width
                                int w;
                                for (w = 1; i+w<width && mask[n+w]==m; w++)
                                {
                                }

                                // Compute height
                                bool done = false;
                                int k;
                                int h;
                                for (h = 1; j+h<width; h++)
                                {
                                    for (k = 0; k<w; k++)
                                    {
                                        int maskIndex = n+k+h*width;
                                        if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                        {
                                            done = true;
                                            break;
                                        }
                                    }

                                    if (done)
                                        break;
                                }

                                if (ignoreBottomFace && x[1]==0)
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
                                    vecs[0] = new Vector3(x[0], x[1], x[2])-BlockUtils.HalfBlockVector;
                                    vecs[1] = new Vector3(x[0]+du[0], x[1]+du[1], x[2]+du[2])-BlockUtils.HalfBlockVector;
                                    vecs[2] = new Vector3(x[0]+du[0]+dv[0], x[1]+du[1]+dv[1], x[2]+du[2]+dv[2])-BlockUtils.HalfBlockVector;
                                    vecs[3] = new Vector3(x[0]+dv[0], x[1]+dv[1], x[2]+dv[2])-BlockUtils.HalfBlockVector;

                                    {
                                        LocalPools pool = chunk.pools;
                                        VertexData[] vertexData = pool.VertexDataArrayPool.Pop(4);
                                        VertexDataFixed[] vertexDataFixed = pool.VertexDataFixedArrayPool.Pop(4);
                                        {
                                            for (int ii = 0; ii<4; ii++)
                                            {
                                                vertexData[ii] = pool.VertexDataPool.Pop();
                                                vertexData[ii].Vertex = vecs[ii];
                                                vertexDataFixed[ii] = VertexDataUtils.ClassToStruct(vertexData[ii]);
                                            }

                                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexDataFixed, backFace);

                                            for (int ii = 0; ii<4; ii++)
                                                pool.VertexDataPool.Push(vertexData[ii]);
                                        }
                                        pool.VertexDataFixedArrayPool.Push(vertexDataFixed);
                                        pool.VertexDataArrayPool.Push(vertexData);
                                    }
                                }

                                // Zero out the mask
                                int l;
                                for (l = 0; l<h; ++l)
                                {
                                    for (k = 0; k<w; ++k)
                                    {
                                        mask[n+k+l*width] = false;
                                    }
                                }

                                i += w;
                                n += w;
                            }
                        }
                    }
                }
            }

            chunk.pools.BoolArrayPool.Push(mask);
            chunk.pools.Vector3ArrayPool.Push(vecs);
        }
    }
}