using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
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

        private int m_chunkLoadRadiusPrev;
        private int m_chunkDeleteRadiusPrev;

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
            m_chunkLoadRadiusPrev = ChunkLoadRadius;
            m_chunkDeleteRadiusPrev = ChunkDeleteRadius;

            UpdateViewerPosition();
            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
            m_viewerPos += Vector3Int.one;
        }

        void Update()
        {
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
            
            int minY = m_viewerPos.y+world.config.minY;
            int maxY = m_viewerPos.y+world.config.maxY;

            // Cycle through the array of positions
            for (int i = 0; i<m_chunkPositions.Length; i++)
            {
                // Create and register chunks
                for (int y = minY; y<=maxY; y += Env.ChunkSize)
                {
                    // Translate array postions to world/chunk positions
                    Vector3Int newChunkPos = new Vector3Int(
                        (m_chunkPositions[i].x<<Env.ChunkPower)+ m_viewerPos.x,
                        (m_chunkPositions[i].y<<Env.ChunkPower)+y,
                        (m_chunkPositions[i].z<<Env.ChunkPower)+ m_viewerPos.z
                        );

                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(newChunkPos, out chunk, false))
                        continue;

                    m_updateRequests.Add(chunk);
                }
            }
        }

        // The ugliest thing... Until I come with an idea of how to efficiently detect whether a chunk is partialy
        // inside camera frustum, all chunks are going to be marked as potentially visible on the first run
        private bool m_firstRun = true;

        public void ProcessChunks()
        {
            // Process removal requests
            for (int i = 0; i<m_updateRequests.Count;)
            {
                Chunk chunk = m_updateRequests[i];

                ProcessChunk(chunk);

                // Process chunk events
                chunk.UpdateChunk();

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

            if(m_updateRequests.Count>0)
                m_firstRun = false;
        }

        public void ProcessChunk(Chunk chunk)
        {
            // Remove the chunk if it is too far away
            int xd = Mathf.Abs((m_viewerPos.x-chunk.pos.x)>>Env.ChunkPower);
            int zd = Mathf.Abs((m_viewerPos.z-chunk.pos.z)>>Env.ChunkPower);
            if (xd*xd+zd*zd>=ChunkDeleteRadius*ChunkDeleteRadius)
            {
                chunk.stateManager.RequestState(ChunkState.Remove);
                return;
            }

            // Update visibility information
            bool isInsideFrustum = IsChunkInViewFrustum(chunk) || m_firstRun;

            ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;
            stateManager.Visible = isInsideFrustum;
            stateManager.PossiblyVisible = isInsideFrustum;
        }

        private void UpdateRanges()
        {
            // Make sure ranges are always correct
            ChunkLoadRadius = Mathf.Max(MinRange, ChunkLoadRadius);
            ChunkLoadRadius = Mathf.Min(MaxRange-1, ChunkLoadRadius);
            if (ChunkDeleteRadius <= ChunkLoadRadius)
                ChunkDeleteRadius = ChunkDeleteRadius + 1;
            ChunkDeleteRadius = Mathf.Max(MinRange+1, ChunkDeleteRadius);
            ChunkDeleteRadius = Mathf.Min(MaxRange, ChunkDeleteRadius);

            bool isDifference = ChunkLoadRadius != m_chunkLoadRadiusPrev || m_chunkPositions == null;
            m_chunkLoadRadiusPrev = ChunkLoadRadius;

            if (isDifference)
                m_chunkPositions = ChunkLoadOrder.ChunkPositions(ChunkLoadRadius);
            if (isDifference || ChunkDeleteRadius!=m_chunkDeleteRadiusPrev)
                m_viewerPos = m_viewerPos + Vector3Int.one; // Invalidate prev pos so that updated ranges can take effect right away
        }

        private void UpdateViewerPosition()
        {
            Vector3Int pos = Chunk.ContainingCoordinates(transform.position);

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;
            m_viewerPos = new Vector3Int(pos.x, FollowCamera ? pos.y : 0, pos.z);
        }

        private bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return !UseFrustumCulling || GeometryUtility.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);
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
                        int xd = Mathf.Abs((m_viewerPos.x-pos.x)>>Env.ChunkPower);
                        int zd = Mathf.Abs((m_viewerPos.z-pos.z)>>Env.ChunkPower);
                        if (xd*xd+zd*zd>=ChunkDeleteRadius*ChunkDeleteRadius)
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
