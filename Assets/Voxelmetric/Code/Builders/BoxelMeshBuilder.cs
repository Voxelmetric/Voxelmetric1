using UnityEngine;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Builders
{
    /// <summary>
    /// Generates a typical cubical voxel geometry for a chunk
    /// </summary>
    public class BoxelMeshBuilder: IMeshBuilder
    {
        public void Build(Chunk chunk)
        {
            WorldBlocks blocks = chunk.world.blocks;
            Block airBlock = chunk.world.blockProvider.BlockTypes[0];

            int stepSize = 1;
            int width = Env.ChunkSize;

            int[] mins = {0, 0, 0};
            int[] maxes = {Env.ChunkMask, Env.ChunkMask, Env.ChunkMask};

            int[] x = {0, 0, 0}; // Relative position of a block
            int[] q = {0, 0, 0}; // Direction in which we compare neighbors when building mask (q[d] is our current direction)
            int[] du = {0, 0, 0}; // Width in a given dimension (du[u] is our current dimension)
            int[] dv = {0, 0, 0}; // Height in a given dimension (dv[v] is our current dimension)

            Block[] mask = chunk.pools.PopBlockArray(width*width);
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
                    //case 0: dir = Direction.west; break;
                    case 3: dir = Direction.east; break;

                    case 1: dir = Direction.down; break;
                    case 4: dir = Direction.up; break;

                    case 2: dir = Direction.south; break;
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
                            mask[n++] = airBlock;
                    }

                    for (x[v] = mins[v]; x[v]<=maxes[v]; x[v]++)
                    {
                        for (x[u] = 0; x[u]<mins[u]; x[u]++)
                            mask[n++] = airBlock;

                        for (x[u] = mins[u]; x[u]<=maxes[u]; x[u]++)
                        {
                            int realX = x[0];
                            int realY = x[1];
                            int realZ = x[2];

                            Block voxelFace0 = blocks.GetBlock(new BlockPos(realX, realY, realZ));
                            Block voxelFace1 = blocks.GetBlock(new BlockPos(realX+q[0], realY+q[1], realZ+q[2]));

                            mask[n++] = 
                                //voxelFace0.CanBuildFaceWith(voxelFace1, dir) ? (backFace ? voxelFace1 : voxelFace0) : airBlock;
                                voxelFace0.solid && voxelFace1.solid ? airBlock : (backFace ? voxelFace1 : voxelFace0);
                        }

                        for (x[u] = maxes[u]+1; x[u]<width; x[u]++)
                            mask[n++] = airBlock;
                    }

                    for (x[v] = maxes[v]+1; x[v]<width; x[v]++)
                    {
                        for (x[u] = 0; x[u]<width; x[u]++)
                            mask[n++] = airBlock;
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
                            if (mask[n]==airBlock)
                            {
                                i++;
                                n++;
                                continue;
                            }

                            // Compute width & height
                            const int w = 1;
                            const int h = 1;

                            // Determine whether we really want to build this face
                            // TODO: Skip bottom faces at the bottom of the world
                            bool buildFace = true;
                            if (buildFace)
                            {
                                // Prepare face coordinates and dimensions
                                x[u] = i;
                                x[v] = j;
                                
                                du[0] = du[1] = du[2] = 0;
                                dv[0] = dv[1] = dv[2] = 0;
                                du[u] = w;
                                dv[v] = h;

                                // Face vertices
                                BlockPos v1 = new BlockPos(
                                    x[0], x[1], x[2]
                                    );
                                BlockPos v2 = new BlockPos(
                                    x[0]+du[0], x[1]+du[1], x[2]+du[2]
                                    );
                                BlockPos v3 = new BlockPos(
                                    x[0]+du[0]+dv[0], x[1]+du[1]+dv[1], x[2]+du[2]+dv[2]
                                    );
                                BlockPos v4 = new BlockPos(
                                    x[0]+dv[0], x[1]+dv[1], x[2]+dv[2]
                                    );

                                // Face vertices transformed to world coordinates
                                // 0--1
                                // |  |
                                // |  |
                                // 3--2
                                vecs[0] = new Vector3(v4.x, v4.y, v4.z);
                                vecs[1] = new Vector3(v3.x, v3.y, v3.z);
                                vecs[2] = new Vector3(v2.x, v2.y, v2.z);
                                vecs[3] = new Vector3(v1.x, v1.y, v1.z);

                                // Build the face
                                Block b = mask[n];
                                BlockPos localPos = new BlockPos(x[0], x[1], x[2]);
                                BlockPos globalPos = localPos + chunk.pos;
                                b.BuildFace(chunk, vecs, localPos, globalPos, dir);
                            }

                            // Zero out the mask
                            int l;
                            for (l = 0; l<h; ++l)
                            {
                                int k;
                                for (k = 0; k<w; ++k)
                                {
                                    mask[n+k+l*width] = airBlock;
                                }
                            }

                            i += w;
                            n += w;
                        }
                    }
                }
            }

            chunk.pools.PushBlockArray(mask);
            chunk.pools.PushVector3Array(vecs);
        }
    }
}
