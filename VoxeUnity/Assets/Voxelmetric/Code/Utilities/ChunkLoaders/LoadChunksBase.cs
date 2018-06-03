using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using UnityEngine.Profiling;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Clipmap;
using Voxelmetric.Code.Data_types;
using Chunk = Voxelmetric.Code.Core.Chunk;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public abstract class LoadChunksBase : MonoBehaviour
    {
        protected const int HorizontalMinRange = 0;
        protected const int HorizontalMaxRange = 32;
        protected const int HorizontalDefRange = 6;
        protected const int VerticalMinRange = 0;
        protected const int VerticalMaxRange = 32;
        protected const int VerticalDefRange = 3;

        //! The world we are attached to
        public World world;
        //! The camera against which we perform frustrum checks
        protected Camera m_camera;
        //! Position of the camera when the game started
        protected Vector3 m_cameraStartPos;

        //! Distance in chunks for loading chunks
        [Range(HorizontalMinRange, HorizontalMaxRange)] public int HorizontalChunkLoadRadius = HorizontalDefRange;
        //! Distance in chunks for loading chunks
        [Range(VerticalMinRange, VerticalMaxRange)] public int VerticalChunkLoadRadius = VerticalDefRange;
        //! Makes the world regenerate around the attached camera. If false, X sticks at 0.
        public bool FollowCameraX = true;
        //! Makes the world regenerate around the attached camera. If false, Y sticks at 0.
        public bool FollowCameraY = false;
        //! Makes the world regenerate around the attached camera. If false, Z sticks at 0.
        public bool FollowCameraZ = true;
        //! Toogles frustum culling
        public bool UseFrustumCulling = true;
        //! If false, only visible part of map is loaded on startup
        public bool FullLoadOnStartUp = true;

        public bool Diag_DrawWorldBounds = false;
        public bool Diag_DrawLoadRange = false;

        protected int m_chunkHorizontalLoadRadiusPrev;
        protected int m_chunkVerticalLoadRadiusPrev;

        protected Vector3Int[] m_chunkPositions;
        protected readonly Plane[] m_cameraPlanes = new Plane[6];
        protected Vector3Int m_viewerPos;
        protected Vector3Int m_viewerPosPrev;

        //! A list of chunks to update
        protected readonly List<Chunk> m_updateRequests = new List<Chunk>();

        protected virtual void OnPreProcessChunks() { }
        protected virtual void UpdateVisibility(int x, int y, int z, int rangeX, int rangeY, int rangeZ) { }
        protected abstract void OnProcessChunk(Chunk chunk);

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
            m_viewerPosPrev += Vector3Int.one;
        }

        void Update()
        {
            Globals.GeometryBudget.Reset();
            Globals.SetBlockBudget.Reset();

            PreProcessChunks();
            PostProcessChunks();
            ProcessChunks();
        }

        public void PreProcessChunks()
        {
            Profiler.BeginSample("PreProcessChunks");

            // Recalculate camera frustum planes
            Planes.CalculateFrustumPlanes(m_camera, m_cameraPlanes);

            // Update clipmap based on range values
            UpdateRanges();

            // Update viewer position
            UpdateViewerPosition();

            OnPreProcessChunks();

            Profiler.EndSample();
        }

        public void PostProcessChunks()
        {
            int minX = m_viewerPos.x - (HorizontalChunkLoadRadius * Env.ChunkSize);
            int maxX = m_viewerPos.x + (HorizontalChunkLoadRadius * Env.ChunkSize);
            int minY = m_viewerPos.y - (VerticalChunkLoadRadius * Env.ChunkSize);
            int maxY = m_viewerPos.y + (VerticalChunkLoadRadius * Env.ChunkSize);
            int minZ = m_viewerPos.z - (HorizontalChunkLoadRadius * Env.ChunkSize);
            int maxZ = m_viewerPos.z + (HorizontalChunkLoadRadius * Env.ChunkSize);
            world.CapCoordXInsideWorld(ref minX, ref maxX);
            world.CapCoordYInsideWorld(ref minY, ref maxY);
            world.CapCoordZInsideWorld(ref minZ, ref maxZ);

            world.Bounds = new AABBInt(minX, minY, minZ, maxX, maxY, maxZ);

            int expectedChunks = m_chunkPositions.Length * ((maxY - minY + Env.ChunkSize) / Env.ChunkSize);

            if (// No update necessary if there was no movement
                m_viewerPos == m_viewerPosPrev &&
                // However, we need to make sure that we have enough chunks loaded
                world.Count >= expectedChunks)
                return;

            // Unregister any non-necessary pending structures
            Profiler.BeginSample("UnregisterStructures");
            {
                world.UnregisterPendingStructures();
            }
            Profiler.EndSample();

            // Cycle through the array of positions
            Profiler.BeginSample("PostProcessChunks");
            {
                // Cycle through the array of positions
                for (int y = maxY; y >= minY; y -= Env.ChunkSize)
                {
                    for (int i = 0; i < m_chunkPositions.Length; i++)
                    {
                        // Skip loading chunks which are off limits
                        int cx = (m_chunkPositions[i].x * Env.ChunkSize) + m_viewerPos.x;
                        if (cx > maxX || cx < minX)
                            continue;
                        int cy = (m_chunkPositions[i].y * Env.ChunkSize) + y;
                        if (cy > maxY || cy < minY)
                            continue;
                        int cz = (m_chunkPositions[i].z * Env.ChunkSize) + m_viewerPos.z;
                        if (cz > maxZ || cz < minZ)
                            continue;

                        // Create a new chunk if possible
                        Vector3Int newChunkPos = new Vector3Int(cx, cy, cz);
                        Chunk chunk;
                        if (!world.CreateChunk(ref newChunkPos, out chunk))
                            continue;

                        if (FullLoadOnStartUp)
                        {
                            chunk.PossiblyVisible = true;
                            chunk.NeedsRenderGeometry = false;
                        }

                        m_updateRequests.Add(chunk);
                    }
                }
            }
            Profiler.EndSample();
        }

        private void HandleVisibility()
        {
            if (!UseFrustumCulling)
                return;

            Profiler.BeginSample("CullPrepare1");

            // Make everything invisible by default
            foreach (var ch in world.Chunks)
            {
                ch.PossiblyVisible = false;
                ch.NeedsRenderGeometry = false;
            }

            Profiler.EndSample();

            int minX = m_viewerPos.x - (HorizontalChunkLoadRadius * Env.ChunkSize);
            int maxX = m_viewerPos.x + (HorizontalChunkLoadRadius * Env.ChunkSize);
            int minY = m_viewerPos.y - (VerticalChunkLoadRadius * Env.ChunkSize);
            int maxY = m_viewerPos.y + (VerticalChunkLoadRadius * Env.ChunkSize);
            int minZ = m_viewerPos.z - (HorizontalChunkLoadRadius * Env.ChunkSize);
            int maxZ = m_viewerPos.z + (HorizontalChunkLoadRadius * Env.ChunkSize);
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
            UpdateVisibility(minX, minY, minZ, maxX - minX + 1, maxY - minY + 1, maxZ - minZ + 1);
        }

        public void ProcessChunks()
        {
            Profiler.BeginSample("ProcessChunks");

            HandleVisibility();

            // Process removal requests
            for (int i = 0; i < m_updateRequests.Count;)
            {
                Chunk chunk = m_updateRequests[i];

                OnProcessChunk(chunk);

                // Update the chunk if possible
                if (chunk.Update())
                {
                    // Build geometry if there is enough time
                    if (Globals.GeometryBudget.HasTimeBudget)
                    {
                        Globals.GeometryBudget.StartMeasurement();

                        bool wasBuilt = chunk.UpdateCollisionGeometry();
                        wasBuilt |= chunk.UpdateRenderGeometry();
                        if (wasBuilt)
                            Globals.GeometryBudget.StopMeasurement();
                    }
                }

                // Automatically collect chunks which are ready to be removed from the world
                if (chunk.IsStateCompleted(ChunkState.Remove))
                {
                    // Remove the chunk from our provider and unregister it from chunk storage
                    world.RemoveChunk(chunk);

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
                OnUpdateRanges();

                m_viewerPosPrev = m_viewerPos + Vector3Int.one; // Invalidate prev pos so that updated ranges can take effect right away
            }
        }

        protected virtual void OnUpdateRanges()
        {
        }

        private void UpdateViewerPosition()
        {
            Vector3Int chunkPos = transform.position;
            Vector3Int pos = Helpers.ContainingChunkPos(ref chunkPos);

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;

            // Do not let y overflow
            int x = m_viewerPos.x;
            if (FollowCameraX)
            {
                x = pos.x;
                world.CapCoordXInsideWorld(ref x, ref x);
            }

            // Do not let y overflow
            int y = m_viewerPos.y;
            if (FollowCameraY)
            {
                y = pos.y;
                world.CapCoordYInsideWorld(ref y, ref y);
            }

            // Do not let y overflow
            int z = m_viewerPos.z;
            if (FollowCameraZ)
            {
                z = pos.z;
                world.CapCoordZInsideWorld(ref z, ref z);
            }

            m_viewerPos = new Vector3Int(x, y, z);
        }
    }
}
