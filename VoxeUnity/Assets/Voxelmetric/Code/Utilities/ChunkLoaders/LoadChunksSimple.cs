using UnityEngine;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Chunk = Voxelmetric.Code.Core.Chunk;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    public class LoadChunksSimple: LoadChunksBase
    {
        protected override void UpdateVisibility(int x, int y, int z, int rangeX, int rangeY, int rangeZ)
        {
            if (rangeX==0 || rangeY==0 || rangeZ==0)
                return;

            Profiler.BeginSample("Cull");

            bool isLast = rangeX==1 && rangeY==1 && rangeZ==1;
            
            int wx = x*Env.ChunkSize;
            int wy = y*Env.ChunkSize;
            int wz = z*Env.ChunkSize;

            // Stop if there is no further subdivision possible
            if (isLast)
            {
                Profiler.BeginSample("CullLast");

                // Update chunk's visibility information
                Vector3Int chunkPos = new Vector3Int(wx, wy, wz);
                Chunk chunk = world.GetChunk(ref chunkPos);
                if (chunk != null)
                {
                    // Update visibility information
                    bool isVisible = Planes.TestPlanesAABB(m_cameraPlanes, ref chunk.WorldBounds);
                    chunk.NeedsRenderGeometry = isVisible;
                    chunk.PossiblyVisible = isVisible;
                }

                Profiler.EndSample(); // CullLast
                Profiler.EndSample(); // Cull
                return;
            }
            
            int rx = rangeX*Env.ChunkSize;
            int ry = rangeY*Env.ChunkSize;
            int rz = rangeZ*Env.ChunkSize;

            // Check whether the bouding box lies inside the camera's frustum
            AABB bounds2 = new AABB(wx, wy, wz, wx+rx, wy+ry, wz+rz);
            int inside = Planes.TestPlanesAABB2(m_cameraPlanes, ref bounds2);

            #region Full invisibility            

            // Everything is invisible by default

            #endregion

            #region Full visibility            
            
            if (inside==6)
            {
                Profiler.BeginSample("CullFullInside");

                // Full visibility. All chunks in this area need to be made visible
                for (int cy = wy; cy<wy+ry; cy += Env.ChunkSize)
                {
                    for (int cz = wz; cz<wz+rz; cz += Env.ChunkSize)
                    {
                        for (int cx = wx; cx<wx+rx; cx += Env.ChunkSize)
                        {
                            // Update chunk's visibility information
                            Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                            Chunk chunk = world.GetChunk(ref chunkPos);
                            if (chunk==null)
                                continue;
                            
                            // Update visibility information
                            chunk.PossiblyVisible = true;
                            chunk.NeedsRenderGeometry = true;
                        }
                    }
                }

                Profiler.EndSample(); // CullLast
                Profiler.EndSample(); // Cull
                return;
            }

            #endregion

            #region Partial visibility

            int offX = rangeX;
            if (rangeX>1)
            {
                offX = rangeX>>1;
                rangeX = rangeX - offX;
            }
            int offY = rangeY;
            if (rangeY>1)
            {
                offY = rangeY>>1;
                rangeY = rangeY - offY;
            }
            int offZ = rangeZ;
            if (rangeZ>1)
            {
                offZ = rangeZ>>1;
                rangeZ = rangeZ - offZ;
            }

            Profiler.EndSample();

            // Subdivide if possible
            // TODO: Avoid the recursion
            UpdateVisibility(x     , y     , z     , offX  , offY  , offZ);
            UpdateVisibility(x+offX, y     , z     , rangeX, offY  , offZ);
            UpdateVisibility(x     , y     , z+offZ, offX  , offY  , rangeZ);
            UpdateVisibility(x+offX, y     , z+offZ, rangeX, offY  , rangeZ);
            UpdateVisibility(x     , y+offY, z     , offX  , rangeY, offZ);
            UpdateVisibility(x+offX, y+offY, z     , rangeX, rangeY, offZ);
            UpdateVisibility(x     , y+offY, z+offZ, offX  , rangeY, rangeZ);
            UpdateVisibility(x+offX, y+offY, z+offZ, rangeX, rangeY, rangeZ);

            #endregion
        }

        protected override void OnProcessChunk(Chunk chunk)
        {
            Profiler.BeginSample("ProcessChunk");
            
            int xd = (m_viewerPos.x-chunk.Pos.x)/Env.ChunkSize;
            int yd = (m_viewerPos.y-chunk.Pos.y)/Env.ChunkSize;
            int zd = (m_viewerPos.z-chunk.Pos.z)/Env.ChunkSize;

            // Remove the chunk if it is too far away
            if (
                !ChunkLoadOrder.CheckXZ(xd, zd, HorizontalChunkLoadRadius) ||
                !ChunkLoadOrder.CheckY(yd, VerticalChunkLoadRadius)
                )
            {
                chunk.RequestRemoval();
            }
            else
            {
                // Dummy collider example - create a collider for chunks directly surrounding the viewer
                chunk.NeedsColliderGeometry = Helpers.Abs(xd)<=1 && Helpers.Abs(yd)<=1 && Helpers.Abs(zd)<=1;

                if (!UseFrustumCulling)
                {
                    // Update visibility information
                    chunk.NeedsRenderGeometry = true;
                    chunk.PossiblyVisible = true;
                }
            }

            Profiler.EndSample();
        }
                
        private void OnDrawGizmosSelected()
        {
            if (!enabled)
                return;

            float size = Env.ChunkSize*Env.BlockSize;
            float halfSize = size*0.5f;
            float smallSize = size*0.25f;

            if (world!=null && (Diag_DrawWorldBounds || Diag_DrawLoadRange))
            {
                foreach (Chunk chunk in world.Chunks)
                {
                    if (Diag_DrawWorldBounds)
                    {
                        // Make central chunks more apparent by using yellow color
                        bool isCentral = chunk.Pos.x==m_viewerPos.x || chunk.Pos.y==m_viewerPos.y || chunk.Pos.z==m_viewerPos.z;
                        Gizmos.color = isCentral ? Color.yellow : Color.blue;
                        Vector3 chunkCenter = new Vector3(
                            chunk.Pos.x+(Env.ChunkSize>>1),
                            chunk.Pos.y+(Env.ChunkSize>>1),
                            chunk.Pos.z+(Env.ChunkSize>>1)
                            );
                        Vector3 chunkSize = new Vector3(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize);
                        Gizmos.DrawWireCube(chunkCenter, chunkSize);
                    }

                    if (Diag_DrawLoadRange)
                    {
                        Vector3Int pos = chunk.Pos;
                        int xd = (m_viewerPos.x - chunk.Pos.x) / Env.ChunkSize;
                        int zd = (m_viewerPos.z - chunk.Pos.z) / Env.ChunkSize;

                        if (ChunkLoadOrder.CheckXZ(xd, zd, HorizontalChunkLoadRadius))
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.Pos.x+halfSize, 0, chunk.Pos.z+halfSize),
                                new Vector3(size-1f, 0, size-1f)
                                );
                        }
                        else
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.Pos.x+halfSize, 0, chunk.Pos.z+halfSize),
                                new Vector3(size-1f, 0, size-1f)
                                );
                        }

                        // Show generated chunks
                        if (chunk.IsStateCompleted(ChunkState.Generate))
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.Pos.x+halfSize, 0, chunk.Pos.z+halfSize),
                                new Vector3(smallSize-0.05f, 0, smallSize-0.05f)
                                );
                        }
                    }
                }
            }
        }
    }
}
