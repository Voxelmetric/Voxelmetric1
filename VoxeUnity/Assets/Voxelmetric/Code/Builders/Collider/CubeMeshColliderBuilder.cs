using UnityEngine;
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
            ChunkBlocks blocks = chunk.blocks;

            int[] mins = {minX, minY, minZ};
            int[] maxes = {maxX, maxY, maxZ};

            int[] x = {0, 0, 0}; // Relative position of a block
            int[] q = {0, 0, 0};
            // Direction in which we compare neighbors when building mask (q[d] is our current direction)
            int[] du = {0, 0, 0}; // Width in a given dimension (du[u] is our current dimension)
            int[] dv = {0, 0, 0}; // Height in a given dimension (dv[v] is our current dimension)

            bool[] mask = chunk.pools.PopBoolArray(width*width);
            Vector3[] vecs = chunk.pools.PopVector3Array(4);

            // Iterate over 3 dimensions. Once for front faces, once for back faces
            for (int dd = 0; dd<2*3; dd++)
            {
                int d = dd%3;
                int u = (d+1)%3;
                int v = (d+2)%3;

                x[0] = 0;
                x[1] = 0;
                x[2] = 0;

                q[0] = 0;
                q[1] = 0;
                q[2] = 0;
                q[d] = stepSize;

                // Determine which side we're meshing
                Direction dir = Direction.west;
                switch (dd)
                {
                    // Back faces
                    //case 0: dir = Direction.west; break;
                    case 1: dir = Direction.down; break;
                    case 2: dir = Direction.south; break;

                    // Front faces
                    case 3: dir = Direction.east; break;
                    case 4: dir = Direction.up; break;
                    case 5: dir = Direction.north; break;
                }
                bool backFace = DirectionUtils.Backface(dir);

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
                            bool voxelFace1 = blocks.GetBlock(new Vector3Int(realX+q[0], realY+q[1], realZ+q[2])).canBeWalkedOn;

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
                            for (w = 1; i + w < width && mask[n + w] == m; w++)
                            {
                            }

                            // Compute height
                            bool done = false;
                            int k;
                            int h;
                            for (h = 1; j + h < width; h++)
                            {
                                for (k = 0; k < w; k++)
                                {
                                    int maskIndex = n+k+h*width;
                                    if (mask[maskIndex] ==false || mask[maskIndex] != m)
                                    {
                                        done = true;
                                        break;
                                    }
                                }

                                if (done)
                                    break;
                            }

                            // Determine whether we really want to build this face
                            // TODO: Skip bottom faces at the bottom of the world
                            const bool buildFace = true;
                            if (buildFace)
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
                                    VertexData[] vertexData = pool.PopVertexDataArray(4);
                                    VertexDataFixed[] vertexDataFixed = pool.PopVertexDataFixedArray(4);
                                    {
                                        for (int ii = 0; ii<4; ii++)
                                        {
                                            vertexData[ii] = pool.PopVertexData();
                                            vertexData[ii].Vertex = vecs[ii];
                                            vertexDataFixed[ii] = VertexDataUtils.ClassToStruct(vertexData[ii]);
                                        }

                                        chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexDataFixed, backFace);

                                        for (int ii = 0; ii<4; ii++)
                                            pool.PushVertexData(vertexData[ii]);
                                    }
                                    pool.PushVertexDataFixedArray(vertexDataFixed);
                                    pool.PushVertexDataArray(vertexData);
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

            chunk.pools.PushBoolArray(mask);
            chunk.pools.PushVector3Array(vecs);
        }
    }
}