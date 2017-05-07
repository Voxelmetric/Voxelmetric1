using System;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Configurable.Blocks.Utilities;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;

namespace Voxelmetric.Code.Builders.Collider
{
    /// <summary>
    /// Generates a cubical collider mesh with merged faces
    /// </summary>
    public class CubeMeshColliderBuilder: MergedFacesMeshBuilder
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
            bool canBeWalkedOn = block.CanBeWalkedOn;

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

            VertexData[] vertexData = pools.VertexDataArrayPool.PopExact(4);
            bool[] mask = pools.BoolArrayPool.PopExact(sideSize*sideSize);

            int n, w, h, l, k, maskIndex;

            // Top
            if (listeners[(int)Direction.up]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
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
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, maxY, zz);
                        bool neighborCanBeWalkedOn = blocks.GetBlock(neighborIndex).CanBeWalkedOn;

                        // Let's see whether we can merge these faces
                        if ((!canBeWalkedOn || !neighborCanBeWalkedOn) && canBeWalkedOn)
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
                            vertexData[0].Vertex = new Vector3(xx, maxY, zz) +
                                                   new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[1].Vertex = new Vector3(xx, maxY, zz+h) +
                                                   new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[2].Vertex = new Vector3(xx+w, maxY, zz+h) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[3].Vertex = new Vector3(xx+w, maxY, zz) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);

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
            // Bottom
            if (listeners[(int)Direction.down]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
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
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, minY-1, zz);
                        bool neighborCanBeWalkedOn = blocks.GetBlock(neighborIndex).CanBeWalkedOn;

                        // Let's see whether we can merge these faces
                        if ((!canBeWalkedOn || !neighborCanBeWalkedOn) && canBeWalkedOn)
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
                            vertexData[0].Vertex = new Vector3(xx, minY, zz) +
                                                   new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[1].Vertex = new Vector3(xx, minY, zz+h) +
                                                   new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[2].Vertex = new Vector3(xx+w, minY, zz+h) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[3].Vertex = new Vector3(xx+w, minY, zz) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);

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
            // Right
            if (listeners[(int)Direction.east]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
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
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(maxX, yy, zz);
                        bool neighborCanBeWalkedOn = blocks.GetBlock(neighborIndex).CanBeWalkedOn;

                        // Let's see whether we can merge these faces
                        if ((!canBeWalkedOn || !neighborCanBeWalkedOn) && canBeWalkedOn)
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
                            vertexData[0].Vertex = new Vector3(maxX, yy, zz) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[1].Vertex = new Vector3(maxX, yy+h, zz) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[2].Vertex = new Vector3(maxX, yy+h, zz+w) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[3].Vertex = new Vector3(maxX, yy, zz+w) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);

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
            // Left
            if (listeners[(int)Direction.west]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
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
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(minX-1, yy, zz);
                        bool neighborCanBeWalkedOn = blocks.GetBlock(neighborIndex).CanBeWalkedOn;

                        // Let's see whether we can merge these faces
                        if ((!canBeWalkedOn || !neighborCanBeWalkedOn) && canBeWalkedOn)
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
                            vertexData[0].Vertex = new Vector3(maxX, yy, zz) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[1].Vertex = new Vector3(maxX, yy+h, zz) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[2].Vertex = new Vector3(maxX, yy+h, zz+w) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[3].Vertex = new Vector3(maxX, yy, zz+w) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);

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
            // Front
            if (listeners[(int)Direction.north]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
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
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, yy, maxZ);
                        bool neighborCanBeWalkedOn = blocks.GetBlock(neighborIndex).CanBeWalkedOn;

                        // Let's see whether we can merge these faces
                        if ((!canBeWalkedOn || !neighborCanBeWalkedOn) && canBeWalkedOn)
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
                            vertexData[0].Vertex = new Vector3(xx, yy, maxZ) +
                                                   new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[1].Vertex = new Vector3(xx, yy+h, maxZ) +
                                                   new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[2].Vertex = new Vector3(xx+w, yy+h, maxZ) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, +BlockUtils.blockPadding);
                            vertexData[3].Vertex = new Vector3(xx+w, yy, maxZ) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, +BlockUtils.blockPadding);

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
            // Back
            if (listeners[(int)Direction.south]!=null ||
                // Don't render faces on world's edges for chunks with no neighbor
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
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(xx, yy, minZ-1);
                        bool neighborCanBeWalkedOn = blocks.GetBlock(neighborIndex).CanBeWalkedOn;

                        // Let's see whether we can merge these faces
                        if ((!canBeWalkedOn || !neighborCanBeWalkedOn) && canBeWalkedOn)
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
                            vertexData[0].Vertex = new Vector3(xx, yy, minZ) +
                                                   new Vector3(-BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[1].Vertex = new Vector3(xx, yy+h, minZ) +
                                                   new Vector3(-BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[2].Vertex = new Vector3(xx+w, yy+h, minZ) +
                                                   new Vector3(+BlockUtils.blockPadding, +BlockUtils.blockPadding, -BlockUtils.blockPadding);
                            vertexData[3].Vertex = new Vector3(xx+w, yy, minZ) +
                                                   new Vector3(+BlockUtils.blockPadding, -BlockUtils.blockPadding, -BlockUtils.blockPadding);

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

            pools.BoolArrayPool.Push(mask);
            pools.VertexDataArrayPool.Push(vertexData);
        }
    }
}
