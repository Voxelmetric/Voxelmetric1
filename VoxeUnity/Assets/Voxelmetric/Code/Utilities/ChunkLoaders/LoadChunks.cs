using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
        private const int HorizontalMinRange = 4;
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

            UpdateViewerPosition();
            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
            m_viewerPos += Vector3Int.one;
        }

        void Update()
        {
            Globals.GeometryBudget.Reset();
            Globals.EdgeSyncBudget.Reset();

            PreProcessChunks();
            ProcessChunks();
            PostProcessChunks();
        }

        public void PreProcessChunks()
        {
            // Recalculate camera frustum planes
            Geometry.CalculateFrustumPlanes(m_camera, ref m_cameraPlanes);

            // Update clipmap based on range values
            UpdateRanges();

            // Update viewer position
            UpdateViewerPosition();

            // Update clipmap offsets based on the viewer position
            m_clipmap.SetOffset(
                m_viewerPos.x >> Env.ChunkPow,
                m_viewerPos.y >> Env.ChunkPow,
                m_viewerPos.z >> Env.ChunkPow
                );
        }

        public void PostProcessChunks()
        {
            // No update necessary if there was no movement
            if (m_viewerPos==m_viewerPosPrev)
                return;

            int minY = m_viewerPos.y-(VerticalChunkLoadRadius<<Env.ChunkPow);
            int maxY = m_viewerPos.y+(VerticalChunkLoadRadius<<Env.ChunkPow);
            world.CapCoordYInsideWorld(ref minY, ref maxY);

            // Cycle through the array of positions
            for (int i = 0; i<m_chunkPositions.Length; i++)
            {
                // Create and register chunks
                for (int y = minY; y<=maxY; y += Env.ChunkSize)
                {
                    // Translate array postions to world/chunk positions
                    Vector3Int newChunkPos = new Vector3Int(
                        (m_chunkPositions[i].x<<Env.ChunkPow)+m_viewerPos.x,
                        (m_chunkPositions[i].y<<Env.ChunkPow)+y,
                        (m_chunkPositions[i].z<<Env.ChunkPow)+m_viewerPos.z
                        );

                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(newChunkPos, out chunk, false))
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
        }

        public void ProcessChunks()
        {
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

            if (m_updateRequests.Count > 0)
                FullLoadOnStartUp = false;
        }

        public void ProcessChunk(Chunk chunk)
        {
            int xd = Helpers.Abs((m_viewerPos.x - chunk.pos.x) >> Env.ChunkPow);
            int yd = Helpers.Abs((m_viewerPos.y - chunk.pos.y) >> Env.ChunkPow);
            int zd = Helpers.Abs((m_viewerPos.z - chunk.pos.z) >> Env.ChunkPow);

            int tx = m_clipmap.TransformX(chunk.pos.x >> Env.ChunkPow);
            int ty = m_clipmap.TransformY(chunk.pos.y >> Env.ChunkPow);
            int tz = m_clipmap.TransformZ(chunk.pos.z >> Env.ChunkPow);

            ChunkStateManagerClient stateManager = chunk.stateManager;

            // Chunk is too far away. Remove it
            if (!m_clipmap.IsInsideBounds_Transformed(tx, ty, tz))
            {
                stateManager.RequestState(ChunkState.Remove);
            }
            else
            {
                // Dummy collider example - create a collider for chunks directly surrounding the viewer
                chunk.NeedsCollider = xd <= 1 && yd <= 1 && zd <= 1;

                // Chunk is within view frustum
                if (UseFrustumCulling)
                {
                    ClipmapItem item = m_clipmap.Get_Transformed(tx, ty, tz);

                    // Chunk is in visibilty range. Full update with geometry generation is possible
                    if (item.IsInVisibleRange)
                    {
                        //chunk.LOD = item.LOD;
                        stateManager.PossiblyVisible = true;
                        stateManager.Visible = IsChunkInViewFrustum(chunk);
                    }
                    // Chunk is cached range. Full update except for geometry generation
                    else
                    {
                        //chunk.LOD = item.LOD;
                        stateManager.PossiblyVisible = true;
                        stateManager.Visible = false;
                    }
                }
                else
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
            HorizontalChunkLoadRadius = Mathf.Min(HorizontalMaxRange - 1, HorizontalChunkLoadRadius);

            // Make sure vertical ranges are always correct
            VerticalChunkLoadRadius = Mathf.Max(VerticalMinRange, VerticalChunkLoadRadius);
            VerticalChunkLoadRadius = Mathf.Min(VerticalMaxRange - 1, VerticalChunkLoadRadius);

            bool isDifferenceXZ = HorizontalChunkLoadRadius != m_chunkHorizontalLoadRadiusPrev || m_chunkPositions == null;
            bool isDifferenceY = VerticalChunkLoadRadius != m_chunkVerticalLoadRadiusPrev;
            m_chunkHorizontalLoadRadiusPrev = HorizontalChunkLoadRadius;
            m_chunkVerticalLoadRadiusPrev = VerticalChunkLoadRadius;

            // Rebuild precomputed chunk positions
            if (isDifferenceXZ)
                m_chunkPositions = ChunkLoadOrder.ChunkPositions(HorizontalChunkLoadRadius+1);
            // Invalidate prev pos so that updated ranges can take effect right away
            if (isDifferenceXZ || isDifferenceY ||
                HorizontalChunkLoadRadius != m_chunkHorizontalLoadRadiusPrev ||
                VerticalChunkLoadRadius != m_chunkVerticalLoadRadiusPrev)
            {
                m_clipmap = new Clipmap(
                    HorizontalChunkLoadRadius, HorizontalChunkLoadRadius+1,
                    VerticalChunkLoadRadius, VerticalChunkLoadRadius+1
                    );
                m_clipmap.Init(0, 0);

                m_viewerPos = m_viewerPos + Vector3Int.one; // Invalidate prev pos so that updated ranges can take effect right away
            }
        }

        private void UpdateViewerPosition()
        {
            Vector3Int pos = Chunk.ContainingCoordinates(transform.position);

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

        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return Geometry.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);
        }

        private void OnDrawGizmosSelected()
        {
            if (!enabled)
                return;

            int size = Mathf.FloorToInt(Env.ChunkSize*Env.BlockSize);
            int halfSize = size>>1;
            int smallSize = size>>4;

            if (world!=null && world.chunks!=null && (Diag_DrawWorldBounds || Diag_DrawLoadRange))
            {
                foreach (Chunk chunk in world.chunks.chunkCollection)
                {
                    if (Diag_DrawWorldBounds)
                    {
                        // Make center chunks more apparent by using yellow color
                        Gizmos.color = chunk.pos.z == 0 || chunk.pos.y == 0 || chunk.pos.z == 0 ? Color.yellow : Color.blue;
                        Gizmos.DrawWireCube(chunk.WorldBounds.center, chunk.WorldBounds.size);
                    }

                    if (Diag_DrawLoadRange)
                    {
                        Vector3Int pos = chunk.pos;

                        if (chunk.pos.y==0)
                        {
                            int tx = m_clipmap.TransformX(pos.x >> Env.ChunkPow);
                            int ty = m_clipmap.TransformY(pos.y >> Env.ChunkPow);
                            int tz = m_clipmap.TransformZ(pos.z >> Env.ChunkPow);

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
                                else
                                {
                                    Gizmos.color = Color.grey;
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
