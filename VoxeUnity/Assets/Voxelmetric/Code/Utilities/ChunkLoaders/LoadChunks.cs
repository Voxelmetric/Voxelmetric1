using System.Collections.Generic;
using Assets.Voxelmetric.Code.Utilities;
using UnityEngine;
using UnityEngine.Assertions;
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
        private const int MinRange = 4;
        private const int MaxRange = 32;

        // The world we are attached to
        public World world;
        // The camera against which we perform frustrum checks
        private Camera m_camera;

        // Distance in chunks for loading chunks
        [Range(MinRange, MaxRange-1)] public int ChunkLoadRadius = 6;
        // Distance in chunks for unloading chunks
        [Range(MinRange+1, MaxRange)] public int ChunkDeleteRadius = 8;
        // Makes the world regenerate around the attached camera. If false, Y sticks at 0.
        public bool FollowCamera;
        // Toogles frustum culling
        public bool UseFrustumCulling;

        public bool Diag_DrawWorldBounds;
        public bool Diag_DrawLoadRange;

        private Vector3Int[] m_chunkPositions;
        private Plane[] m_cameraPlanes = new Plane[6];
        private Clipmap m_clipmap;
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
            UpdateViewerPosition();
            m_viewerPos += Vector3Int.one;
            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
        }

        void Update()
        {
            m_timeBudgetHandler.Reset();

            PreProcessChunks();
            ProcessChunks();
            PostProcessChunks();
        }

        // Updates our clipmap region. Has to be set from the outside!
        private void UpdateRanges()
        {
            // Make sure ranges are always correct
            ChunkLoadRadius = Mathf.Max(MinRange, ChunkLoadRadius);
            ChunkLoadRadius = Mathf.Min(MaxRange - 1, ChunkLoadRadius);
            if (ChunkDeleteRadius <= ChunkLoadRadius)
                ChunkDeleteRadius = ChunkDeleteRadius + 1;
            ChunkDeleteRadius = Mathf.Max(MinRange + 1, ChunkDeleteRadius);
            ChunkDeleteRadius = Mathf.Min(MaxRange, ChunkDeleteRadius);

            if ( // Clipmap not initialized yet
                m_clipmap==null ||
                // Visibility range changed
                ChunkLoadRadius!=m_clipmap.VisibleRange || ChunkDeleteRadius!=m_clipmap.CachedRange
                )
            {
                m_clipmap = new Clipmap(
                    world.config.minY/Env.ChunkSize,
                    world.config.maxY/Env.ChunkSize,
                    ChunkLoadRadius, ChunkDeleteRadius
                    );
                m_clipmap.Init(0, 0);

                m_chunkPositions = ChunkLoadOrder.ChunkPositions(ChunkLoadRadius);
                m_viewerPos = m_viewerPos + Vector3Int.one; // Invalidate prev pos so that updated ranges can take effect right away
            }
        }

        private void UpdateViewerPosition()
        {
            Vector3Int pos = Chunk.ContainingCoordinates(transform.position);
            int posX = pos.x>>Env.ChunkPower;
            int posY = pos.y>>Env.ChunkPower;
            int posZ = pos.z>>Env.ChunkPower;

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;
            m_viewerPos = new Vector3Int(posX, FollowCamera ? posY : 0, posZ);
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
            m_clipmap.SetOffset(m_viewerPos.x, m_viewerPos.y, m_viewerPos.z);
        }

        public void PostProcessChunks()
        {
            // No update necessary if there was no movement
            if (m_viewerPos==m_viewerPosPrev)
                return;

            // Translate the viewer position to world position
            Vector3Int viewerPosInWP = m_viewerPos*Env.ChunkSize;

            int minY = viewerPosInWP.y+world.config.minY;
            int maxY = viewerPosInWP.y+world.config.maxY;

            // Cycle through the array of positions
            for (int i = 0; i<m_chunkPositions.Length; i++)
            {
                // Create and register chunks
                for (int y = minY; y<=maxY; y += Env.ChunkSize)
                {
                    // Translate array postions to world/chunk positions
                    Vector3Int newChunkPos = new Vector3Int(
                        (m_chunkPositions[i].x<<Env.ChunkPower)+viewerPosInWP.x,
                        (m_chunkPositions[i].y<<Env.ChunkPower)+y,
                        (m_chunkPositions[i].z<<Env.ChunkPower)+viewerPosInWP.z
                        );

                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(newChunkPos, out chunk, false))
                        continue;

                    m_updateRequests.Add(chunk);
                }
            }
        }

        // TODO! The ugliest thing... Until I implement an efficient detect of chunks being at least partialy
        // inside the camera frustum, all chunks are going to be marked as potentially visible on the first run
        private bool m_firstRun = true;

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

            if (m_updateRequests.Count > 0)
                m_firstRun = false;
        }

        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return !UseFrustumCulling || GeometryUtility.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);
        }

        public void ProcessChunk(Chunk chunk)
        {
            Vector3Int localChunkPos = new Vector3Int(
                chunk.pos.x>>Env.ChunkPower,
                chunk.pos.y>>Env.ChunkPower,
                chunk.pos.z>>Env.ChunkPower
                );

            ClipmapItem item = m_clipmap[localChunkPos.x, localChunkPos.y, localChunkPos.z];
            ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;

            // Chunk is within view frustum
            if (IsChunkInViewFrustum(chunk) || m_firstRun)
            {
                // Chunk is too far away. Remove it
                if (!m_clipmap.IsInsideBounds(localChunkPos.x, localChunkPos.y, localChunkPos.z))
                {
                    stateManager.RequestState(ChunkState.Remove);
                }
                // Chunk is within visibilty range. Full update with geometry generation is possible
                else if (item.IsWithinVisibleRange)
                {
                    //chunk.LOD = item.LOD;
                    stateManager.PossiblyVisible = true;
                    stateManager.Visible = true;
                }
                // Chunk is within cached range. Full update except for geometry generation
                else // if (item.IsWithinCachedRange)
                {
                    //chunk.LOD = item.LOD;
                    stateManager.PossiblyVisible = true;
                    stateManager.Visible = false;
                }
            }
            else
            {
                // Chunk is not visible and too far away. Remove it
                if (!m_clipmap.IsInsideBounds(localChunkPos.x, localChunkPos.y, localChunkPos.z))
                {
                    stateManager.RequestState(ChunkState.Remove);
                }
                // Chunk is not in the view frustum but still within cached range
                else if (item.IsWithinCachedRange)
                {
                    //chunk.LOD = item.LOD;
                    stateManager.PossiblyVisible = false;
                    stateManager.Visible = false;
                }
                else
                // Weird state
                {
                    Assert.IsFalse(true);
                    stateManager.RequestState(ChunkState.Remove);
                }
            }
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

                        if (chunk.pos.y==0)
                        {
                            Vector3Int localChunkPos = new Vector3Int(
                                pos.x>>Env.ChunkPower,
                                pos.y>>Env.ChunkPower,
                                pos.z>>Env.ChunkPower
                                );

                            ClipmapItem item = m_clipmap[localChunkPos.x, localChunkPos.y, localChunkPos.z];

                            if (!m_clipmap.IsInsideBounds(localChunkPos.x, localChunkPos.y, localChunkPos.z))
                            {
                                Gizmos.color = Color.red;
                                Gizmos.DrawWireCube(
                                    new Vector3(pos.x+halfSize, 0, pos.z+halfSize),
                                    new Vector3(size-0.05f, 0, size-0.05f)
                                    );
                            }
                            else if (item.IsWithinVisibleRange)
                            {
                                Gizmos.color = Color.green;
                                Gizmos.DrawWireCube(
                                    new Vector3(pos.x+halfSize, 0, pos.z+halfSize),
                                    new Vector3(size-0.05f, 0, size-0.05f)
                                    );
                            }
                            else // if (item.IsWithinCachedRange)
                            {
                                Gizmos.color = Color.yellow;
                                Gizmos.DrawWireCube(
                                    new Vector3(pos.x+halfSize, 0, pos.z+halfSize),
                                    new Vector3(size-0.05f, 0, size-0.05f)
                                    );
                            }
                        }

                        // Show generated chunks
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;
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
