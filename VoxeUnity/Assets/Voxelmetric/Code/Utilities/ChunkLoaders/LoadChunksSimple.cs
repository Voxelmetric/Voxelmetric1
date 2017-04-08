using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    [RequireComponent(typeof (Camera))]
    public class LoadChunksSimple: MonoBehaviour, IChunkLoader
    {
        private const int HorizontalMinRange = 2;
        private const int HorizontalMaxRange = 32;
        private const int HorizontalDefRange = 6;
        private const int VerticalMinRange = 2;
        private const int VerticalMaxRange = 32;
        private const int VerticalDefRange = 3;

        // The world we are attached to
        public World world;
        // The camera against which we perform frustrum checks
        private Camera m_camera;

        // Distance in chunks for loading chunks
        [Range(HorizontalMinRange, HorizontalMaxRange)] public int HorizontalChunkLoadRadius = HorizontalDefRange;
        // Distance in chunks for loading chunks
        [Range(VerticalMinRange, VerticalMaxRange)] public int VerticalChunkLoadRadius = VerticalDefRange;
        // Makes the world regenerate around the attached camera. If false, Y sticks at 0.
        public bool FollowCamera;
        // Toogles frustum culling
        public bool UseFrustumCulling;
        // If false, only visible part of map is loaded on startup
        public bool FullLoadOnStartUp = true;

        public bool Diag_DrawWorldBounds;
        public bool Diag_DrawLoadRange;

        private int m_chunkHorizontalLoadRadiusPrev;
        private int m_chunkVerticalLoadRadiusPrev;

        private Vector3Int[] m_chunkPositions;
        private Plane[] m_cameraPlanes = new Plane[6];
        private Vector3Int m_viewerPos;
        private Vector3Int m_viewerPosPrev;
        
        //! A list of chunks to update
        private readonly List<Chunk> m_updateRequests = new List<Chunk>();

        void Awake()
        {
            Assert.IsNotNull(world);
            m_camera = GetComponent<Camera>();
        }

        void Start()
        {
            m_chunkHorizontalLoadRadiusPrev = HorizontalChunkLoadRadius;
            m_chunkVerticalLoadRadiusPrev = VerticalChunkLoadRadius;

            UpdateViewerPosition();
            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
            m_viewerPos += Vector3Int.one;
        }

        void Update()
        {
            Globals.GeometryBudget.Reset();
            Globals.EdgeSyncBudget.Reset();

            PreProcessChunks();
            PostProcessChunks();
            ProcessChunks();
        }

        public void PreProcessChunks()
        {
            Profiler.BeginSample("PreProcessChunks");

            // Recalculate camera frustum planes
            Geometry.CalculateFrustumPlanes(m_camera, ref m_cameraPlanes);

            // Update clipmap based on range values
            UpdateRanges();

            // Update viewer position
            UpdateViewerPosition();

            Profiler.EndSample();
        }
        
        private void UpdateVisibility(int x, int y, int z, int rangeX, int rangeY, int rangeZ)
        {
            if (rangeX==0 || rangeY==0 || rangeZ==0)
                return;

            bool isLast = rangeX==1 && rangeY==1 && rangeZ==1;
            
            int wx = x*Env.ChunkSize;
            int wy = y*Env.ChunkSize;
            int wz = z*Env.ChunkSize;

            // Stop if there is no further subdivision possible
            if (isLast)
            {
                // Update chunk's visibility information
                Vector3Int chunkPos = new Vector3Int(wx, wy, wz);
                Chunk chunk = world.chunks.Get(chunkPos);
                if (chunk==null)
                    return;

                ChunkStateManagerClient stateManager = chunk.stateManager;

                int xd = (m_viewerPos.x-chunk.pos.x)/Env.ChunkSize;
                int yd = (m_viewerPos.y-chunk.pos.y)/Env.ChunkSize;
                int zd = (m_viewerPos.z-chunk.pos.z)/Env.ChunkSize;
                
                int xDist = xd*xd+zd*zd;
                int yDist = yd*yd;
                
                // Update visibility information
                bool isVisible = Geometry.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);

                stateManager.Visible = isVisible &&
                                        xDist<=HorizontalChunkLoadRadius*HorizontalChunkLoadRadius &&
                                        yDist<=VerticalChunkLoadRadius*VerticalChunkLoadRadius;
                stateManager.PossiblyVisible = isVisible || FullLoadOnStartUp;

                return;
            }
            
            int rx = rangeX*Env.ChunkSize;
            int ry = rangeY*Env.ChunkSize;
            int rz = rangeZ*Env.ChunkSize;

            /*AABB bounds2 = new AABB(wx, wy, wz, wx+rx, wy+ry, wz+rz);
            int inside = 0;

            // if the camera position lies inside the bounding box we'll assume partial visibility automatically
            if (bounds2.IsInside(m_camera.transform.position))
                inside = 3;
            else
            {
                // Check whether the bouding box lies inside the camera's frustum
                inside = Geometry.TestPlanesAABB2(m_cameraPlanes, bounds2);
            }*/
            // Check whether the bouding box lies inside the camera's frustum
            AABB bounds2 = new AABB(wx, wy, wz, wx+rx, wy+ry, wz+rz);
            int inside = Geometry.TestPlanesAABB2(m_cameraPlanes, bounds2);

            #region Full invisibility            

            if (inside==0)
            {
                // Full invisibility. All chunks in this area need to be made invisible
                for (int cy = wy; cy<wy+ry; cy += Env.ChunkSize)
                {
                    for (int cz = wz; cz<wz+rz; cz += Env.ChunkSize)
                    {
                        for (int cx = wx; cx<wx+rx; cx += Env.ChunkSize)
                        {
                            // Update chunk's visibility information
                            Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                            Chunk chunk = world.chunks.Get(chunkPos);
                            if (chunk==null)
                                continue;

                            ChunkStateManagerClient stateManager = chunk.stateManager;
                            
                            // Update visibility information
                            stateManager.PossiblyVisible = FullLoadOnStartUp;
                            stateManager.Visible = false;
                        }
                    }
                }

                return;
            }

            #endregion

            #region Full visibility            

            if (inside==6)
            {
                // Full visibility. All chunks in this area need to be made visible
                for (int cy = wy; cy<wy+ry; cy += Env.ChunkSize)
                {
                    for (int cz = wz; cz<wz+rz; cz += Env.ChunkSize)
                    {
                        for (int cx = wx; cx<wx+rx; cx += Env.ChunkSize)
                        {
                            // Update chunk's visibility information
                            Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                            Chunk chunk = world.chunks.Get(chunkPos);
                            if (chunk==null)
                                continue;

                            ChunkStateManagerClient stateManager = chunk.stateManager;

                            int xd = (m_viewerPos.x-chunk.pos.x)/Env.ChunkSize;
                            int yd = (m_viewerPos.y-chunk.pos.y)/Env.ChunkSize;
                            int zd = (m_viewerPos.z-chunk.pos.z)/Env.ChunkSize;
                            
                            int xDist = xd*xd+zd*zd;
                            int yDist = yd*yd;

                            // Update visibility information
                            stateManager.Visible = xDist<=HorizontalChunkLoadRadius*HorizontalChunkLoadRadius &&
                                                   yDist<=VerticalChunkLoadRadius*VerticalChunkLoadRadius;
                            stateManager.PossiblyVisible = true;
                        }
                    }
                }

                return;
            }

            #endregion
            
            #region Partial visibility
            
            int offX = rangeX;
            if (rangeX>1)
            {
                offX = rangeX>>1;
                rangeX = (rangeX+1)>>1; // ceil the number
            }
            int offY = rangeY;
            if (rangeY>1)
            {
                offY = rangeY>>1;
                rangeY = (rangeY+1)>>1; // ceil the number
            }
            int offZ = rangeZ;
            if (rangeZ>1)
            {
                offZ = rangeZ>>1;
                rangeZ = (rangeZ+1)>>1; // ceil the number
            }

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

        private void HandleVisibility()
        {
            if (!UseFrustumCulling)
                return;

            Profiler.BeginSample("HandleVisibility");

            int minY = m_viewerPos.y - (VerticalChunkLoadRadius * Env.ChunkSize);
            int maxY = m_viewerPos.y + (VerticalChunkLoadRadius * Env.ChunkSize);
            world.CapCoordYInsideWorld(ref minY, ref maxY);

            minY /= Env.ChunkSize;
            maxY /= Env.ChunkSize;

            // TODO: Merge this with clipmap
            // Let's update chunk visibility info. Operate in chunk load radius so we know we're never outside cached range
            UpdateVisibility(
                (m_viewerPos.x/Env.ChunkSize)-HorizontalChunkLoadRadius, minY, (m_viewerPos.z/Env.ChunkSize)-HorizontalChunkLoadRadius,
                (HorizontalChunkLoadRadius<<1)+1, maxY-minY+1, (HorizontalChunkLoadRadius<<1)+1
                );

            Profiler.EndSample();
        }

        public void PostProcessChunks()
        {
            // No update necessary if there was no movement
            if (m_viewerPos==m_viewerPosPrev)
                return;

            Profiler.BeginSample("PostProcessChunks");

            int minY = m_viewerPos.y-(VerticalChunkLoadRadius*Env.ChunkSize);
            int maxY = m_viewerPos.y+(VerticalChunkLoadRadius*Env.ChunkSize);
            world.CapCoordYInsideWorld(ref minY, ref maxY);

            // Cycle through the array of positions
            for (int y = maxY; y > minY; y -= Env.ChunkSize)
            {
                for (int i = 0; i < m_chunkPositions.Length; i++)
                {
                    // Translate array postions to world/chunk positions
                    Vector3Int newChunkPos = new Vector3Int(
                        (m_chunkPositions[i].x*Env.ChunkSize)+m_viewerPos.x,
                        (m_chunkPositions[i].y*Env.ChunkSize)+y,
                        (m_chunkPositions[i].z*Env.ChunkSize)+m_viewerPos.z
                        );

                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(newChunkPos, out chunk))
                        continue;

                    if (FullLoadOnStartUp)
                    {
                        ChunkStateManagerClient stateManager = chunk.stateManager;
                        stateManager.PossiblyVisible = true;
                        stateManager.Visible = false;
                    }

                    m_updateRequests.Add(chunk);
                }
            }

            Profiler.EndSample();
        }

        public void ProcessChunks()
        {
            Profiler.BeginSample("ProcessChunks");

            HandleVisibility();

            // Process removal requests
            for (int i = 0; i<m_updateRequests.Count;)
            {
                Chunk chunk = m_updateRequests[i];

                ProcessChunk(chunk);

                // Update the chunk if possible
                if (chunk.CanUpdate)
                {
                    chunk.UpdateState();
                    chunk.UpdateRenderGeometry();
                    chunk.UpdateCollisionGeometry();
                }

                // Automatically collect chunks which are ready to be removed from the world
                ChunkStateManagerClient stateManager = chunk.stateManager;
                if (stateManager.IsStateCompleted(ChunkState.Remove))
                {
                    // Remove the chunk from our provider and unregister it from chunk storage
                    world.chunks.RemoveChunk(chunk);

                    // Unregister from updates
                    m_updateRequests.RemoveAt(i);
                    continue;
                }

                ++i;
            }

            FullLoadOnStartUp = false;

            Profiler.EndSample();
        }

        public void ProcessChunk(Chunk chunk)
        {
            Profiler.BeginSample("ProcessChunk");

            ChunkStateManagerClient stateManager = chunk.stateManager;

            int xd = (m_viewerPos.x-chunk.pos.x)/Env.ChunkSize;
            int yd = (m_viewerPos.y-chunk.pos.y)/Env.ChunkSize;
            int zd = (m_viewerPos.z-chunk.pos.z)/Env.ChunkSize;

            int hRadius = HorizontalChunkLoadRadius+1;
            int vRadius = VerticalChunkLoadRadius+1;
            int xDist = xd*xd+zd*zd;
            int yDist = yd*yd;

            // Remove the chunk if it is too far away
            if (xDist>hRadius*hRadius || yDist>vRadius*vRadius)
            {
                stateManager.RequestState(ChunkState.Remove);
            }
            else
            {
                // Dummy collider example - create a collider for chunks directly surrounding the viewer
                chunk.NeedsCollider = Helpers.Abs(xd)<=1 && Helpers.Abs(yd)<=1 && Helpers.Abs(zd)<=1;

                if (!UseFrustumCulling)
                {
                    // Update visibility information
                    stateManager.Visible = xDist<=HorizontalChunkLoadRadius*HorizontalChunkLoadRadius &&
                                           yDist<=VerticalChunkLoadRadius*VerticalChunkLoadRadius;
                    stateManager.PossiblyVisible = true;
                }
            }

            Profiler.EndSample();
        }

        private void UpdateRanges()
        {
            // Make sure horizontal ranges are always correct
            HorizontalChunkLoadRadius = Mathf.Max(HorizontalMinRange, HorizontalChunkLoadRadius);
            HorizontalChunkLoadRadius = Mathf.Min(HorizontalMaxRange-1, HorizontalChunkLoadRadius);

            // Make sure vertical ranges are always correct
            VerticalChunkLoadRadius = Mathf.Max(VerticalMinRange, VerticalChunkLoadRadius);
            VerticalChunkLoadRadius = Mathf.Min(VerticalMaxRange-1, VerticalChunkLoadRadius);

            bool isDifferenceXZ = HorizontalChunkLoadRadius!=m_chunkHorizontalLoadRadiusPrev || m_chunkPositions==null;
            bool isDifferenceY = VerticalChunkLoadRadius!=m_chunkVerticalLoadRadiusPrev;
            m_chunkHorizontalLoadRadiusPrev = HorizontalChunkLoadRadius;
            m_chunkVerticalLoadRadiusPrev = VerticalChunkLoadRadius;

            // Rebuild precomputed chunk positions
            if (isDifferenceXZ)
                m_chunkPositions = ChunkLoadOrder.ChunkPositions(HorizontalChunkLoadRadius+1);
            // Invalidate prev pos so that updated ranges can take effect right away
            if (isDifferenceXZ || isDifferenceY ||
                HorizontalChunkLoadRadius != m_chunkHorizontalLoadRadiusPrev ||
                VerticalChunkLoadRadius != m_chunkVerticalLoadRadiusPrev)
                m_viewerPos = m_viewerPos+Vector3Int.one;
        }

        private void UpdateViewerPosition()
        {
            Vector3Int pos = Chunk.ContainingChunkPos(transform.position);

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;

            // Do not let y overflow
            int y = 0;
            if (FollowCamera)
            {
                y = pos.y;
                world.CapCoordYInsideWorld(ref y, ref y);
            }

            m_viewerPos = new Vector3Int(pos.x, y, pos.z);
        }
        
        private void OnDrawGizmosSelected()
        {
            if (!enabled)
                return;

            float size = Env.ChunkSize*Env.BlockSize;
            float halfSize = size*0.5f;
            float smallSize = size*0.25f;

            if (world!=null && world.chunks!=null && (Diag_DrawWorldBounds || Diag_DrawLoadRange))
            {
                foreach (Chunk chunk in world.chunks.chunkCollection)
                {
                    if (Diag_DrawWorldBounds)
                    {
                        // Make center chunks more apparent by using yellow color
                        Gizmos.color = chunk.pos.z==0 || chunk.pos.y==0 || chunk.pos.z==0 ? Color.yellow : Color.blue;
                        Vector3 chunkCenter = new Vector3(
                            chunk.pos.x+(Env.ChunkSize>>1),
                            chunk.pos.y+(Env.ChunkSize>>1),
                            chunk.pos.z+(Env.ChunkSize>>1)
                            );
                        Vector3 chunkSize = new Vector3(Env.ChunkSize, Env.ChunkSize, Env.ChunkSize);
                        Gizmos.DrawWireCube(chunkCenter, chunkSize);
                    }

                    if (Diag_DrawLoadRange)
                    {
                        Vector3Int pos = chunk.pos;
                        int xd = Helpers.Abs((m_viewerPos.x-pos.x)/Env.ChunkSize);
                        int zd = Helpers.Abs((m_viewerPos.z-pos.z)/Env.ChunkSize);
                        int dist = xd*xd+zd*zd;
                        if (dist<=HorizontalChunkLoadRadius*HorizontalChunkLoadRadius)
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.pos.x+halfSize, 0, chunk.pos.z+halfSize),
                                new Vector3(size-1f, 0, size-1f)
                                );
                        }
                        else if (dist<=(HorizontalChunkLoadRadius+1)*(HorizontalChunkLoadRadius+1))
                        {
                            Gizmos.color = Color.grey;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.pos.x+halfSize, 0, chunk.pos.z+halfSize),
                                new Vector3(size-1f, 0, size-1f)
                                );
                        }
                        else
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.pos.x+halfSize, 0, chunk.pos.z+halfSize),
                                new Vector3(size-1f, 0, size-1f)
                                );
                        }

                        // Show generated chunks
                        ChunkStateManagerClient stateManager = chunk.stateManager;
                        if (stateManager.IsStateCompleted(ChunkState.Generate))
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.pos.x+halfSize, 0, chunk.pos.z+halfSize),
                                new Vector3(smallSize-0.05f, 0, smallSize-0.05f)
                                );
                        }
                    }
                }
            }
        }
    }
}
