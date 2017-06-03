using System;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Builders.Collider
{
    /// <summary>
    /// Generates a cubical collider mesh with merged faces
    /// </summary>
    public class CubeMeshColliderBuilder: MergedFacesMeshBuilder
    {
        protected override bool CanConsiderBlock(Block block)
        {
            return block.CanCollide;
        }

        protected override bool CanCreateBox(Block block, Block neighbor)
        {
            return block.PhysicMaterialID==neighbor.PhysicMaterialID;
        }

        protected override void BuildBox(Chunk chunk, Block block, int minX, int minY, int minZ, int maxX, int maxY, int maxZ)
        {
            // All faces in the are build in the following order:
            //     1--2
            //     |  |
            //     |  |
            //     0--3

            var blocks = chunk.blocks;
            var pools = chunk.pools;
            var listeners = chunk.stateManager.Listeners;

            // Custom blocks have their own rules
            // TODO: Implement custom block colliders
            /*if (block.Custom)
            {
                for (int yy = minY; yy < maxY; yy++)
                {
                    for (int zz = minZ; zz < maxZ; zz++)
                    {
                        for (int xx = minX; xx < maxX; xx++)
                        {
                            ... // build collider here
                        }
                    }
                }

                return;
            }*/

            Vector3[] vertexData = pools.Vector3ArrayPool.PopExact(4);
            bool[] mask = pools.BoolArrayPool.PopExact(sideSize*sideSize);

            int n, w, h, l, k, maskIndex;

            #region Top face
            if (listeners[(int)Direction.up]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                maxY!=Env.ChunkSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // z axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, maxY, minZ);
                int zOffset = Env.ChunkSizeWithPadding-maxX+minX;

                // Build the mask
                for (int zz = minZ; zz<maxZ; ++zz, neighborIndex+=zOffset)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                            mask[n] = true;
                    }
                }

                // Build faces from the mask if it's possible
                for (int zz = minZ; zz<maxZ; ++zz)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n]==false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx+w<sideSize && mask[n+w]==m; w++) ;

                        // Compute height
                        for (h = 1; zz+h<sideSize; h++)
                        {
                            for (k = 0; k<w; k++)
                            {
                                maskIndex = n+k+h*sideSize;
                                if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, maxY, zz) + BlockUtils.PaddingOffsets[(int)Direction.up][0];
                            vertexData[1] = new Vector3(xx, maxY, zz+h) + BlockUtils.PaddingOffsets[(int)Direction.up][1];
                            vertexData[2] = new Vector3(xx+w, maxY, zz+h) + BlockUtils.PaddingOffsets[(int)Direction.up][2];
                            vertexData[3] = new Vector3(xx+w, maxY, zz) + BlockUtils.PaddingOffsets[(int)Direction.up][3];

                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexData, DirectionUtils.IsBackface(Direction.up), block.PhysicMaterialID);
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = false;
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            #endregion

            #region Bottom face
            if (listeners[(int)Direction.down]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                minY!=0)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // z axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, minY-1, minZ);
                int zOffset = Env.ChunkSizeWithPadding-maxX+minX;

                // Build the mask
                for (int zz = minZ; zz<maxZ; ++zz, neighborIndex+=zOffset)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                            mask[n] = true;
                    }
                }

                // Build faces from the mask if it's possible
                for (int zz = minZ; zz<maxZ; ++zz)
                {
                    n = minX+zz*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n]==false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx+w<sideSize && mask[n+w]==m; w++) ;

                        // Compute height
                        for (h = 1; zz+h<sideSize; h++)
                        {
                            for (k = 0; k<w; k++)
                            {
                                maskIndex = n+k+h*sideSize;
                                if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, minY, zz) + BlockUtils.PaddingOffsets[(int)Direction.down][0];
                            vertexData[1] = new Vector3(xx, minY, zz+h) + BlockUtils.PaddingOffsets[(int)Direction.down][1];
                            vertexData[2] = new Vector3(xx+w, minY, zz+h) + BlockUtils.PaddingOffsets[(int)Direction.down][2];
                            vertexData[3] = new Vector3(xx+w, minY, zz) + BlockUtils.PaddingOffsets[(int)Direction.down][3];

                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexData, DirectionUtils.IsBackface(Direction.down), block.PhysicMaterialID);
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = false;
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            #endregion

            #region Right face
            if (listeners[(int)Direction.east]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                maxX!=Env.ChunkSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // y axis - height
                // z axis - width

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(maxX, minY, minZ);
                int yOffset = Env.ChunkSizeWithPaddingPow2-(maxZ-minZ)*Env.ChunkSizeWithPadding;

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy, neighborIndex+=yOffset)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ; ++zz, ++n, neighborIndex+=Env.ChunkSizeWithPadding)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                            mask[n] = true;
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ;)
                    {
                        if (mask[n]==false)
                        {
                            ++zz;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; zz+w<sideSize && mask[n+w]==m; w++) ;

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            for (k = 0; k<w; k++)
                            {
                                maskIndex = n+k+h*sideSize;
                                if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(maxX, yy, zz) + BlockUtils.PaddingOffsets[(int)Direction.east][0];
                            vertexData[1] = new Vector3(maxX, yy+h, zz) + BlockUtils.PaddingOffsets[(int)Direction.east][1];
                            vertexData[2] = new Vector3(maxX, yy+h, zz+w) + BlockUtils.PaddingOffsets[(int)Direction.east][2];
                            vertexData[3] = new Vector3(maxX, yy, zz+w) + BlockUtils.PaddingOffsets[(int)Direction.east][3];

                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexData, DirectionUtils.IsBackface(Direction.east), block.PhysicMaterialID);
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = false;
                        }

                        zz += w;
                        n += w;
                    }
                }
            }
            #endregion

            #region Left face
            if (listeners[(int)Direction.west]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                minX!=0)
            {
                Array.Clear(mask, 0, mask.Length);

                // y axis - height
                // z axis - width

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX-1, minY, minZ);
                int yOffset = Env.ChunkSizeWithPaddingPow2-(maxZ-minZ)*Env.ChunkSizeWithPadding;

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy, neighborIndex+=yOffset)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ; ++zz, ++n, neighborIndex+=Env.ChunkSizeWithPadding)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                            mask[n] = true;
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minZ+yy*sideSize;
                    for (int zz = minZ; zz<maxZ;)
                    {
                        if (mask[n]==false)
                        {
                            ++zz;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; zz+w<sideSize && mask[n+w]==m; w++) ;

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            for (k = 0; k<w; k++)
                            {
                                maskIndex = n+k+h*sideSize;
                                if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(minX, yy, zz) + BlockUtils.PaddingOffsets[(int)Direction.west][0];
                            vertexData[1] = new Vector3(minX, yy+h, zz) + BlockUtils.PaddingOffsets[(int)Direction.west][1];
                            vertexData[2] = new Vector3(minX, yy+h, zz+w) + BlockUtils.PaddingOffsets[(int)Direction.west][2];
                            vertexData[3] = new Vector3(minX, yy, zz+w) + BlockUtils.PaddingOffsets[(int)Direction.west][3];

                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexData, DirectionUtils.IsBackface(Direction.west), block.PhysicMaterialID);
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = false;
                        }

                        zz += w;
                        n += w;
                    }
                }
            }
            #endregion

            #region Front face
            if (listeners[(int)Direction.north]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                maxZ!=Env.ChunkSize)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // y axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, minY, maxZ);
                int yOffset = Env.ChunkSizeWithPaddingPow2-maxX+minX;

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy, neighborIndex+=yOffset)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                            mask[n] = true;
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n]==false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx+w<sideSize && mask[n+w]==m; w++) ;

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            for (k = 0; k<w; k++)
                            {
                                maskIndex = n+k+h*sideSize;
                                if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, yy, maxZ) + BlockUtils.PaddingOffsets[(int)Direction.north][0];
                            vertexData[1] = new Vector3(xx, yy+h, maxZ) + BlockUtils.PaddingOffsets[(int)Direction.north][1];
                            vertexData[2] = new Vector3(xx+w, yy+h, maxZ) + BlockUtils.PaddingOffsets[(int)Direction.north][2];
                            vertexData[3] = new Vector3(xx+w, yy, maxZ) + BlockUtils.PaddingOffsets[(int)Direction.north][3];

                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexData, DirectionUtils.IsBackface(Direction.north), block.PhysicMaterialID);
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = false;
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            #endregion

            #region Back face
            if (listeners[(int)Direction.south]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
                minZ!=0)
            {
                Array.Clear(mask, 0, mask.Length);

                // x axis - width
                // y axis - height

                int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX, minY, minZ-1);
                int yOffset = Env.ChunkSizeWithPaddingPow2-maxX+minX;

                // Build the mask
                for (int yy = minY; yy<maxY; ++yy, neighborIndex+=yOffset)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX; ++xx, ++n, ++neighborIndex)
                    {
                        // Let's see whether we can merge the faces
                        if (!blocks.GetBlock(neighborIndex).CanCollide)
                            mask[n] = true;
                    }
                }

                // Build faces from the mask if it's possible
                for (int yy = minY; yy<maxY; ++yy)
                {
                    n = minX+yy*sideSize;
                    for (int xx = minX; xx<maxX;)
                    {
                        if (mask[n]==false)
                        {
                            ++xx;
                            ++n;
                            continue;
                        }

                        bool m = mask[n];

                        // Compute width
                        for (w = 1; xx+w<sideSize && mask[n+w]==m; w++) ;

                        // Compute height
                        for (h = 1; yy+h<sideSize; h++)
                        {
                            for (k = 0; k<w; k++)
                            {
                                maskIndex = n+k+h*sideSize;
                                if (mask[maskIndex]==false || mask[maskIndex]!=m)
                                    goto cont;
                            }
                        }
                        cont:

                        // Build the face
                        {
                            vertexData[0] = new Vector3(xx, yy, minZ) + BlockUtils.PaddingOffsets[(int)Direction.south][0];
                            vertexData[1] = new Vector3(xx, yy+h, minZ) + BlockUtils.PaddingOffsets[(int)Direction.south][1];
                            vertexData[2] = new Vector3(xx+w, yy+h, minZ) + BlockUtils.PaddingOffsets[(int)Direction.south][2];
                            vertexData[3] = new Vector3(xx+w, yy, minZ) + BlockUtils.PaddingOffsets[(int)Direction.south][3];

                            chunk.ChunkColliderGeometryHandler.Batcher.AddFace(vertexData, DirectionUtils.IsBackface(Direction.south), block.PhysicMaterialID);
                        }

                        // Zero out the mask. We don't need to process the same fields again
                        for (l = 0; l<h; ++l)
                        {
                            maskIndex = n+l*sideSize;
                            for (k = 0; k<w; ++k, ++maskIndex)
                                mask[maskIndex] = false;
                        }

                        xx += w;
                        n += w;
                    }
                }
            }
            #endregion

            pools.BoolArrayPool.Push(mask);
            pools.Vector3ArrayPool.Push(vertexData);
        }
    }
}
