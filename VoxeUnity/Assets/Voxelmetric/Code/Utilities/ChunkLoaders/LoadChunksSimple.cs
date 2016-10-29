using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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
        private const int HorizontalMinRange = 4;
        private const int HorizontalMaxRange = 32;
        private const int VerticalMinRange = 2;
        private const int VerticalMaxRange = 32;

        // The world we are attached to
        public World world;
        // The camera against which we perform frustrum checks
        private Camera m_camera;

        // Distance in chunks for loading chunks
        [Range(HorizontalMinRange, HorizontalMaxRange-1)] public int HorizontalChunkLoadRadius = 6;
        // Distance in chunks for unloading chunks
        [Range(HorizontalMinRange+1, HorizontalMaxRange)] public int HorizontalChunkDeleteRadius = 8;
        // Distance in chunks for loading chunks
        [Range(VerticalMinRange, VerticalMaxRange - 1)] public int VerticalChunkLoadRadius = 3;
        // Distance in chunks for unloading chunks
        [Range(VerticalMinRange + 1, VerticalMaxRange)] public int VerticalChunkDeleteRadius = 4;
        // Makes the world regenerate around the attached camera. If false, Y sticks at 0.
        public bool FollowCamera;
        // Toogles frustum culling
        public bool UseFrustumCulling;
        // If false, only visible part of map is loaded on startup
        public bool FullLoadOnStartUp = true;

        public bool Diag_DrawWorldBounds;
        public bool Diag_DrawLoadRange;

        private int m_chunkHorizontalLoadRadiusPrev;
        private int m_chunkHorizontalDeleteRadiusPrev;
        private int m_chunkVerticalLoadRadiusPrev;
        private int m_chunkVerticalDeleteRadiusPrev;

        private Vector3Int[] m_chunkPositions;
        private Plane[] m_cameraPlanes = new Plane[6];
        private Vector3Int m_viewerPos;
        private Vector3Int m_viewerPosPrev;

        private readonly TimeBudgetHandler m_timeBudgetHandler = new TimeBudgetHandler();

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
            m_chunkHorizontalDeleteRadiusPrev = HorizontalChunkDeleteRadius;
            m_chunkVerticalLoadRadiusPrev = VerticalChunkLoadRadius;
            m_chunkVerticalDeleteRadiusPrev = VerticalChunkDeleteRadius;

            UpdateViewerPosition();
            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
            m_viewerPos += Vector3Int.one;

            m_timeBudgetHandler.TimeBudgetMs = 3; // Time allow to be spent for building meshes
        }

        void Update()
        {
            m_timeBudgetHandler.Reset();

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
        }

        public void PostProcessChunks()
        {
            // No update necessary if there was no movement
            if (m_viewerPos==m_viewerPosPrev)
                return;

            int minY = m_viewerPos.y-(VerticalChunkLoadRadius<<Env.ChunkPower);
            int maxY = m_viewerPos.y+(VerticalChunkLoadRadius<<Env.ChunkPower);
            if (world.config.minY!=world.config.maxY)
            {
                minY = Mathf.Max(minY, world.config.minY);
                maxY = Mathf.Min(maxY, world.config.maxY);
            }

            // Cycle through the array of positions
            for (int i = 0; i<m_chunkPositions.Length; i++)
            {
                // Create and register chunks
                for (int y = minY; y<=maxY; y += Env.ChunkSize)
                {
                    // Translate array postions to world/chunk positions
                    Vector3Int newChunkPos = new Vector3Int(
                        (m_chunkPositions[i].x<<Env.ChunkPower)+m_viewerPos.x,
                        (m_chunkPositions[i].y<<Env.ChunkPower)+y,
                        (m_chunkPositions[i].z<<Env.ChunkPower)+m_viewerPos.z
                        );

                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(newChunkPos, out chunk, false))
                        continue;

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
                    if (m_timeBudgetHandler.HasTimeBudget)
                    {
                        m_timeBudgetHandler.StartMeasurement();

                        bool wasBuilt = chunk.UpdateRenderGeometry();
                        wasBuilt |= chunk.UpdateCollisionGeometry();
                        if (wasBuilt)
                            m_timeBudgetHandler.StopMeasurement();
                    }
                }

                // Automatically collect chunks which are ready to be removed from the world
                ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;
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

            if (m_updateRequests.Count>0)
                FullLoadOnStartUp = false;
        }

        public void ProcessChunk(Chunk chunk)
        {
            int xd = Helpers.Abs((m_viewerPos.x-chunk.pos.x)>>Env.ChunkPower);
            int yd = Helpers.Abs((m_viewerPos.y-chunk.pos.y)>>Env.ChunkPower);
            int zd = Helpers.Abs((m_viewerPos.z-chunk.pos.z)>>Env.ChunkPower);

            // Remove the chunk if it is too far away
            if (
                xd*xd+zd*zd>=HorizontalChunkDeleteRadius*HorizontalChunkDeleteRadius ||
                yd*yd>=VerticalChunkDeleteRadius*VerticalChunkDeleteRadius
                )
            {
                chunk.stateManager.RequestState(ChunkState.Remove);
                return;
            }

            // Dummy collider example - create a collider for chunks directly surrounding the viewer
            chunk.NeedsCollider = xd<=1 && yd<=1 && zd<=1;

            // Update visibility information
            bool isInsideFrustum = FullLoadOnStartUp || IsChunkInViewFrustum(chunk);

            ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;
            stateManager.Visible = isInsideFrustum;
            stateManager.PossiblyVisible = isInsideFrustum;
        }

        private void UpdateRanges()
        {
            // Make sure horizontal ranges are always correct
            HorizontalChunkLoadRadius = Mathf.Max(HorizontalMinRange, HorizontalChunkLoadRadius);
            HorizontalChunkLoadRadius = Mathf.Min(HorizontalMaxRange-1, HorizontalChunkLoadRadius);
            if (HorizontalChunkDeleteRadius<=HorizontalChunkLoadRadius)
                HorizontalChunkDeleteRadius = HorizontalChunkDeleteRadius+1;
            HorizontalChunkDeleteRadius = Mathf.Max(HorizontalMinRange+1, HorizontalChunkDeleteRadius);
            HorizontalChunkDeleteRadius = Mathf.Min(HorizontalMaxRange, HorizontalChunkDeleteRadius);

            // Make sure vertical ranges are always correct
            VerticalChunkLoadRadius = Mathf.Max(VerticalMinRange, VerticalChunkLoadRadius);
            VerticalChunkLoadRadius = Mathf.Min(VerticalMaxRange-1, VerticalChunkLoadRadius);
            if (VerticalChunkDeleteRadius<=VerticalChunkLoadRadius)
                VerticalChunkDeleteRadius = VerticalChunkDeleteRadius+1;
            VerticalChunkDeleteRadius = Mathf.Max(VerticalMinRange+1, VerticalChunkDeleteRadius);
            VerticalChunkDeleteRadius = Mathf.Min(VerticalMaxRange, VerticalChunkDeleteRadius);

            bool isDifferenceXZ = HorizontalChunkLoadRadius!=m_chunkHorizontalLoadRadiusPrev || m_chunkPositions==null;
            bool isDifferenceY = VerticalChunkLoadRadius!=m_chunkVerticalLoadRadiusPrev;
            m_chunkHorizontalLoadRadiusPrev = HorizontalChunkLoadRadius;
            m_chunkVerticalLoadRadiusPrev = VerticalChunkLoadRadius;

            // Rebuild precomputed chunk positions
            if (isDifferenceXZ)
                m_chunkPositions = ChunkLoadOrder.ChunkPositions(HorizontalChunkLoadRadius);
            // Invalidate prev pos so that updated ranges can take effect right away
            if (isDifferenceXZ || isDifferenceY ||
                HorizontalChunkDeleteRadius!=m_chunkHorizontalDeleteRadiusPrev ||
                VerticalChunkDeleteRadius!=m_chunkVerticalDeleteRadiusPrev)
                m_viewerPos = m_viewerPos+Vector3Int.one;
        }

        private void UpdateViewerPosition()
        {
            Vector3Int pos = Chunk.ContainingCoordinates(transform.position);

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;

            // Do not let y overflow
            int y = FollowCamera ? pos.y : 0;
            if (world.config.minY!=world.config.maxY)
            {
                y = Mathf.Max(y, world.config.minY);
                y = Mathf.Min(y, world.config.maxY);
            }

            m_viewerPos = new Vector3Int(pos.x, y, pos.z);
        }

        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return !UseFrustumCulling || Geometry.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);
        }

        private void OnDrawGizmosSelected()
        {
            int size = Mathf.FloorToInt(Env.ChunkSize*Env.BlockSize);
            int halfSize = size>>1;
            int smallSize = size>>4;

            if (world!=null && world.chunks!=null && (Diag_DrawWorldBounds || Diag_DrawLoadRange))
            {
                foreach (Chunk chunk in world.chunks.chunkCollection)
                {
                    if (Diag_DrawWorldBounds)
                    {
                        Gizmos.color = Color.blue;
                        Gizmos.DrawWireCube(chunk.WorldBounds.center, chunk.WorldBounds.size);
                    }

                    if (Diag_DrawLoadRange)
                    {
                        Vector3Int pos = chunk.pos;
                        int xd = Helpers.Abs((m_viewerPos.x-pos.x)>>Env.ChunkPower);
                        int zd = Helpers.Abs((m_viewerPos.z-pos.z)>>Env.ChunkPower);
                        if (xd*xd+zd*zd>=HorizontalChunkDeleteRadius*HorizontalChunkDeleteRadius)
                        {
                            Gizmos.color = Color.red;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.pos.x+halfSize, 0, chunk.pos.z+halfSize),
                                new Vector3(size-0.05f, 0, size-0.05f)
                                );
                        }
                        else
                        {
                            Gizmos.color = Color.green;
                            Gizmos.DrawWireCube(
                                new Vector3(chunk.pos.x+halfSize, 0, chunk.pos.z+halfSize),
                                new Vector3(size-0.05f, 0, size-0.05f)
                                );
                        }

                        // Show generated chunks
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;
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
