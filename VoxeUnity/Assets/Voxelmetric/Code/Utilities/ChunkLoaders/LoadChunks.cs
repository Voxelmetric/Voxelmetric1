using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Clipmap;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    [RequireComponent(typeof (Camera))]
    public class LoadChunks: MonoBehaviour, IChunkLoader
    {
        private const int HorizontalMinRange = 0;
        private const int HorizontalMaxRange = 32;
        private const int HorizontalDefRange = 6;
        private const int VerticalMinRange = 0;
        private const int VerticalMaxRange = 32;
        private const int VerticalDefRange = 3;

        //! The world we are attached to
        public World world;
        //! The camera against which we perform frustrum checks
        private Camera m_camera;
        //! Position of the camera when the game started
        private Vector3 m_cameraStartPos;

        //! Distance in chunks for loading chunks
        [Range(HorizontalMinRange, HorizontalMaxRange)] public int HorizontalChunkLoadRadius = HorizontalDefRange;
        //! Distance in chunks for loading chunks
        [Range(VerticalMinRange, VerticalMaxRange)] public int VerticalChunkLoadRadius = VerticalDefRange;
        //! Makes the world regenerate around the attached camera. If false, X sticks at 0.
        public bool FollowCameraX;
        //! Makes the world regenerate around the attached camera. If false, Y sticks at 0.
        public bool FollowCameraY;
        //! Makes the world regenerate around the attached camera. If false, Z sticks at 0.
        public bool FollowCameraZ;
        //! Toogles frustum culling
        public bool UseFrustumCulling;
        //! If false, only visible part of map is loaded on startup
        public bool FullLoadOnStartUp = true;

        public bool Diag_DrawWorldBounds;
        public bool Diag_DrawLoadRange;

        private int m_chunkHorizontalLoadRadiusPrev;
        private int m_chunkVerticalLoadRadiusPrev;

        private Vector3Int[] m_chunkPositions;
        private Plane[] m_cameraPlanes = new Plane[6];
        private Clipmap m_clipmap;
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

            m_cameraStartPos = m_camera.transform.position;

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

            // Update clipmap offsets based on the viewer position
            m_clipmap.SetOffset(
                m_viewerPos.x / Env.ChunkSize,
                m_viewerPos.y / Env.ChunkSize,
                m_viewerPos.z / Env.ChunkSize
                );


            Profiler.EndSample();
        }
        
        private void UpdateVisibility(int x, int y, int z, int rangeX, int rangeY, int rangeZ)
        {
            if (rangeX == 0 || rangeY == 0 || rangeZ == 0)
                return;

            bool isLast = rangeX==1 && rangeY==1 && rangeZ==1;

            int wx = m_viewerPos.x+(x*Env.ChunkSize);
            int wy = m_viewerPos.y+(y*Env.ChunkSize);
            int wz = m_viewerPos.z+(z*Env.ChunkSize);

            int rx = rangeX*Env.ChunkSize;
            int ry = rangeY*Env.ChunkSize;
            int rz = rangeZ*Env.ChunkSize;

            // Stop if there is no further subdivision possible
            if (isLast)
            {
                // Update chunk's visibility information
                Vector3Int chunkPos = new Vector3Int(wx, wy, wz);
                Chunk chunk = world.chunks.Get(ref chunkPos);
                if (chunk==null)
                    return;

                ChunkStateManagerClient stateManager = chunk.stateManager;

                int tx = m_clipmap.TransformX(x);
                int ty = m_clipmap.TransformY(y);
                int tz = m_clipmap.TransformZ(z);

                // Skip chunks which are too far away
                if (!m_clipmap.IsInsideBounds_Transformed(tx, ty, tz))
                    return;

                // Update visibility information
                ClipmapItem item = m_clipmap.Get_Transformed(tx, ty, tz);
                bool isVisible = Geometry.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);

                stateManager.Visible = isVisible && item.IsInVisibleRange;
                stateManager.PossiblyVisible = isVisible || FullLoadOnStartUp;

                return;
            }
            
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
                            Chunk chunk = world.chunks.Get(ref chunkPos);
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
                            Chunk chunk = world.chunks.Get(ref chunkPos);
                            if (chunk==null)
                                continue;

                            ChunkStateManagerClient stateManager = chunk.stateManager;

                            int tx = m_clipmap.TransformX(x);
                            int ty = m_clipmap.TransformY(y);
                            int tz = m_clipmap.TransformZ(z);

                            // Update visibility information
                            ClipmapItem item = m_clipmap.Get_Transformed(tx, ty, tz);

                            stateManager.Visible = item.IsInVisibleRange;
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

            int minX = m_viewerPos.x-(HorizontalChunkLoadRadius*Env.ChunkSize);
            int maxX = m_viewerPos.x+(HorizontalChunkLoadRadius*Env.ChunkSize);
            int minY = m_viewerPos.y-(VerticalChunkLoadRadius*Env.ChunkSize);
            int maxY = m_viewerPos.y+(VerticalChunkLoadRadius*Env.ChunkSize);
            int minZ = m_viewerPos.z-(HorizontalChunkLoadRadius*Env.ChunkSize);
            int maxZ = m_viewerPos.z+(HorizontalChunkLoadRadius*Env.ChunkSize);
            world.CapCoordXInsideWorld(ref minX, ref maxX);
            world.CapCoordYInsideWorld(ref minY, ref maxY);
            world.CapCoordZInsideWorld(ref minZ, ref maxZ);

            minX /= Env.ChunkSize;
            maxX /= Env.ChunkSize;
            minY /= Env.ChunkSize;
            maxY /= Env.ChunkSize;
            minZ /= Env.ChunkSize;
            maxZ /= Env.ChunkSize;

            // TODO: Merge this with clipmap
            // Let's update chunk visibility info. Operate in chunk load radius so we know we're never outside cached range
            UpdateVisibility(minX, minY, minZ, maxX-minX+1, maxY-minY+1, maxZ-minZ+1);

            Profiler.EndSample();
        }

        public void PostProcessChunks()
        {
            int minX = m_viewerPos.x-(HorizontalChunkLoadRadius*Env.ChunkSize);
            int maxX = m_viewerPos.x+(HorizontalChunkLoadRadius*Env.ChunkSize);
            int minY = m_viewerPos.y-(VerticalChunkLoadRadius*Env.ChunkSize);
            int maxY = m_viewerPos.y+(VerticalChunkLoadRadius*Env.ChunkSize);
            int minZ = m_viewerPos.z-(HorizontalChunkLoadRadius*Env.ChunkSize);
            int maxZ = m_viewerPos.z+(HorizontalChunkLoadRadius*Env.ChunkSize);
            world.CapCoordXInsideWorld(ref minX, ref maxX);
            world.CapCoordYInsideWorld(ref minY, ref maxY);
            world.CapCoordZInsideWorld(ref minZ, ref maxZ);

            world.Bounds = new AABBInt(minX, minY, minZ, maxX, maxY, maxZ);

            int expectedChunks = m_chunkPositions.Length*((maxY-minY+Env.ChunkSize) /Env.ChunkSize);
            
            if (// No update necessary if there was no movement
                m_viewerPos ==m_viewerPosPrev &&
                // However, we need to make sure that we have enough chunks loaded
                world.chunks.Count>=expectedChunks)
                return;

            Profiler.BeginSample("PostProcessChunks");

            // Cycle through the array of positions
            for (int y = maxY; y >= minY; y -= Env.ChunkSize)
            {
                for (int i = 0; i < m_chunkPositions.Length; i++)
                {
                    // Skip loading chunks which are off limits
                    int cx = (m_chunkPositions[i].x*Env.ChunkSize)+m_viewerPos.x;
                    if (cx>maxX || cx<minX)
                        continue;
                    int cy = (m_chunkPositions[i].y*Env.ChunkSize)+y;
                    if (cy>maxY || cy<minY)
                        continue;
                    int cz = (m_chunkPositions[i].z*Env.ChunkSize)+m_viewerPos.z;
                    if (cz>maxZ || cz<minZ)
                        continue;

                    // Create a new chunk if possible
                    Vector3Int newChunkPos = new Vector3Int(cx, cy, cz);
                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(ref newChunkPos, out chunk))
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

                    // Build colliders if there is enough time
                    if (Globals.GeometryBudget.HasTimeBudget)
                    {
                        Globals.GeometryBudget.StartMeasurement();

                        bool wasBuilt = chunk.UpdateRenderGeometry();
                        wasBuilt |= chunk.UpdateCollisionGeometry();
                        if (wasBuilt)
                            Globals.GeometryBudget.StopMeasurement();
                    }
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

            world.PerformBlockActions();

            FullLoadOnStartUp = false;

            Profiler.EndSample();
        }

        public void ProcessChunk(Chunk chunk)
        {
            Profiler.BeginSample("ProcessChunk");

            ChunkStateManagerClient stateManager = chunk.stateManager;

            int tx = m_clipmap.TransformX(chunk.pos.x / Env.ChunkSize);
            int ty = m_clipmap.TransformY(chunk.pos.y / Env.ChunkSize);
            int tz = m_clipmap.TransformZ(chunk.pos.z / Env.ChunkSize);

            // Chunk is too far away. Remove it
            if (!m_clipmap.IsInsideBounds_Transformed(tx, ty, tz))
            {
                stateManager.RequestState(ChunkState.Remove);
            }
            else
            {
                // Dummy collider example - create a collider for chunks directly surrounding the viewer
                int xd = Helpers.Abs((m_viewerPos.x - chunk.pos.x) / Env.ChunkSize);
                int yd = Helpers.Abs((m_viewerPos.y - chunk.pos.y) / Env.ChunkSize);
                int zd = Helpers.Abs((m_viewerPos.z - chunk.pos.z) / Env.ChunkSize);
                chunk.NeedsCollider = xd <= 1 && yd <= 1 && zd <= 1;

                if (!UseFrustumCulling)
                {
                    ClipmapItem item = m_clipmap.Get_Transformed(tx, ty, tz);

                    // Chunk is in visibilty range. Full update with geometry generation is possible
                    if (item.IsInVisibleRange)
                    {
                        //chunk.LOD = item.LOD;
                        stateManager.PossiblyVisible = true;
                        stateManager.Visible = true;
                    }
                    // Chunk is in cached range. Full update except for geometry generation
                    else
                    {
                        //chunk.LOD = item.LOD;
                        stateManager.PossiblyVisible = true;
                        stateManager.Visible = false;
                    }
                }
            }
        }

        // Updates our clipmap region. Has to be set from the outside!
        private void UpdateRanges()
        {
            // Make sure horizontal ranges are always correct
            HorizontalChunkLoadRadius = Mathf.Max(HorizontalMinRange, HorizontalChunkLoadRadius);
            HorizontalChunkLoadRadius = Mathf.Min(HorizontalMaxRange, HorizontalChunkLoadRadius);

            // Make sure vertical ranges are always correct
            VerticalChunkLoadRadius = Mathf.Max(VerticalMinRange, VerticalChunkLoadRadius);
            VerticalChunkLoadRadius = Mathf.Min(VerticalMaxRange, VerticalChunkLoadRadius);

            bool isDifferenceXZ = HorizontalChunkLoadRadius != m_chunkHorizontalLoadRadiusPrev || m_chunkPositions == null;
            bool isDifferenceY = VerticalChunkLoadRadius != m_chunkVerticalLoadRadiusPrev;
            m_chunkHorizontalLoadRadiusPrev = HorizontalChunkLoadRadius;
            m_chunkVerticalLoadRadiusPrev = VerticalChunkLoadRadius;

            // Rebuild precomputed chunk positions
            if (isDifferenceXZ)
                m_chunkPositions = ChunkLoadOrder.ChunkPositions(HorizontalChunkLoadRadius);
            // Invalidate prev pos so that updated ranges can take effect right away
            if (isDifferenceXZ || isDifferenceY ||
                HorizontalChunkLoadRadius != m_chunkHorizontalLoadRadiusPrev ||
                VerticalChunkLoadRadius != m_chunkVerticalLoadRadiusPrev)
            {
                m_clipmap = new Clipmap(
                    HorizontalChunkLoadRadius,
                    VerticalChunkLoadRadius,
                    VerticalChunkLoadRadius+1
                    );
                m_clipmap.Init(0, 0);

                m_viewerPos = m_viewerPos + Vector3Int.one; // Invalidate prev pos so that updated ranges can take effect right away
            }
        }

        private void UpdateViewerPosition()
        {
            Vector3Int chunkPos = transform.position;
            Vector3Int pos = Chunk.ContainingChunkPos(ref chunkPos);

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;

            // Do not let y overflow
            int x = (int)m_cameraStartPos.x;
            if (FollowCameraX)
            {
                x = pos.x;
                world.CapCoordXInsideWorld(ref x, ref x);
            }

            // Do not let y overflow
            int y = (int)m_cameraStartPos.y;
            if (FollowCameraY)
            {
                y = pos.y;
                world.CapCoordYInsideWorld(ref y, ref y);
            }

            // Do not let y overflow
            int z = (int)m_cameraStartPos.z;
            if (FollowCameraZ)
            {
                z = pos.z;
                world.CapCoordZInsideWorld(ref z, ref z);
            }

            m_viewerPos = new Vector3Int(x, y, z);
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
                        // Make central chunks more apparent by using yellow color
                        bool isCentral = chunk.pos.x==m_viewerPos.x || chunk.pos.y==m_viewerPos.y || chunk.pos.z==m_viewerPos.z;
                        Gizmos.color = isCentral ? Color.yellow : Color.blue;
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

                        if (chunk.pos.y==0)
                        {
                            int tx = m_clipmap.TransformX(pos.x / Env.ChunkSize);
                            int ty = m_clipmap.TransformY(pos.y / Env.ChunkSize);
                            int tz = m_clipmap.TransformZ(pos.z / Env.ChunkSize);

                            if (!m_clipmap.IsInsideBounds_Transformed(tx,ty,tz))
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawWireCube(
                                    new Vector3(pos.x+halfSize, 0, pos.z+halfSize),
                                    new Vector3(size-1f, 0, size-1f)
                                    );
                            }
                            else
                            {
                                ClipmapItem item = m_clipmap.Get_Transformed(tx,ty,tz);
                                if (item.IsInVisibleRange)
                                {
                                    Gizmos.color = Color.green;
                                    Gizmos.DrawWireCube(
                                        new Vector3(pos.x+halfSize, 0, pos.z+halfSize),
                                        new Vector3(size-1f, 0, size-1f)
                                        );
                                }
                            }
                        }

                        // Show generated chunks
                        ChunkStateManagerClient stateManager = chunk.stateManager;
                        if (stateManager.IsStateCompleted(ChunkState.Generate))
                        {
                            Gizmos.color = Color.magenta;
                            Gizmos.DrawWireCube(
                                new Vector3(pos.x+halfSize, pos.y+halfSize, pos.z+halfSize),
                                new Vector3(smallSize-0.05f, smallSize-0.05f, smallSize-0.05f)
                                );
                        }
                    }
                }
            }
        }
    }
}
