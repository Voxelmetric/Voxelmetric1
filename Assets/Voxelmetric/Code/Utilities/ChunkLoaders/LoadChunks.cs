using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Math;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Clipmap;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities.ChunkLoaders
{
    /// <summary>
    /// Running constantly, LoadChunks generates the world as we move.
    /// This script can be attached to any component. The world will be loaded based on its position
    /// </summary>
    [RequireComponent(typeof (Camera))]
    public class LoadChunks: MonoBehaviour
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

        private BlockPos[] m_chunkPositions;
        private Plane[] m_cameraPlanes = new Plane[6];
        private Clipmap m_clipmap;
        private BlockPos m_viewerPos;
        private BlockPos m_viewerPosPrev;

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
            m_viewerPos += BlockPos.one;
            // Add some arbirtary value so that m_viewerPosPrev is different from m_viewerPos
        }

        void Update()
        {
            OnPreProcessChunks();
            ProcessUpdateRequests();
            OnPostProcessChunks();
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
                m_viewerPos = m_viewerPos + BlockPos.one; // Invalidate prev pos so that updated ranges can take effect right away
            }
        }

        private void UpdateViewerPosition()
        {
            BlockPos pos = ((BlockPos)transform.position).ContainingChunkCoordinates();
            int posX = pos.x>>Env.ChunkPower;
            int posY = pos.y>>Env.ChunkPower;
            int posZ = pos.z>>Env.ChunkPower;

            // Update the viewer position
            m_viewerPosPrev = m_viewerPos;
            m_viewerPos = new BlockPos(posX, FollowCamera ? posY : 0, posZ);
        }

        private void OnPreProcessChunks()
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

        private void OnPostProcessChunks()
        {
            // No update necessary if there was no movement
            if (m_viewerPos==m_viewerPosPrev)
                return;

            // Translate the viewer position to world position
            BlockPos viewerPosInWP = m_viewerPos*Env.ChunkSize;

            int minY = viewerPosInWP.y+world.config.minY;
            int maxY = viewerPosInWP.y+world.config.maxY;

            // Cycle through the array of positions
            for (int i = 0; i<m_chunkPositions.Length; i++)
            {
                // Create and register chunks
                for (int y = minY; y<=maxY; y += Env.ChunkSize)
                {
                    // Translate array postions to world/chunk positions
                    BlockPos newChunkPos = new BlockPos(
                        (m_chunkPositions[i].x<<Env.ChunkPower)+viewerPosInWP.x,
                        (m_chunkPositions[i].y<<Env.ChunkPower)+y,
                        (m_chunkPositions[i].z<<Env.ChunkPower)+viewerPosInWP.z
                        );

                    Chunk chunk;
                    if (!world.chunks.CreateOrGetChunk(newChunkPos, out chunk))
                        continue;

                    m_updateRequests.Add(chunk);
                }
            }
        }

        // The ugliest thing... Until I come with an idea of how to efficiently detect whether a chunk is partialy
        // inside camera frustum, all chunks are going to be marked as potentially visible on the first run
        private bool m_firstRun = true;

        private void ProcessUpdateRequests()
        {
            // Process removal requests
            for (int i = 0; i<m_updateRequests.Count;)
            {
                Chunk chunk = m_updateRequests[i];

                OnProcessChunk(chunk);

                // Process chunk events
                chunk.UpdateChunk();

                // Automatically collect chunks which are ready to be removed from the world
                if (chunk.stateManager.IsFinished)
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
        
        private void OnProcessChunk(Chunk chunk)
        {
            BlockPos localChunkPos = new BlockPos(
                chunk.pos.x>>Env.ChunkPower,
                chunk.pos.y>>Env.ChunkPower,
                chunk.pos.z>>Env.ChunkPower
                );

            ClipmapItem item = m_clipmap[localChunkPos.x, localChunkPos.y, localChunkPos.z];
            
            // Chunk is within view frustum
            if (IsChunkInViewFrustum(chunk) || m_firstRun)
            {
                // Chunk is too far away. Remove it
                if (!m_clipmap.IsInsideBounds(localChunkPos.x, localChunkPos.y, localChunkPos.z))
                {
                    chunk.stateManager.RequestState(ChunkState.Remove);
                }
                // Chunk is within visibilty range. Full update with geometry generation is possible
                else if (item.IsWithinVisibleRange)
                {
                    //chunk.LOD = item.LOD;
                    chunk.stateManager.PossiblyVisible = true;
                    chunk.stateManager.Visible = true;
                }
                // Chunk is within cached range. Full update except for geometry generation
                else // if (item.IsWithinCachedRange)
                {
                    //chunk.LOD = item.LOD;
                    chunk.stateManager.PossiblyVisible = true;
                    chunk.stateManager.Visible = false;
                }
            }
            else
            {
                // Chunk is not visible and too far away. Remove it
                if (!m_clipmap.IsInsideBounds(localChunkPos.x, localChunkPos.y, localChunkPos.z))
                {
                    chunk.stateManager.RequestState(ChunkState.Remove);
                }
                // Chunk is not in the view frustum but still within cached range
                else if (item.IsWithinCachedRange)
                {
                    //chunk.LOD = item.LOD;
                    chunk.stateManager.PossiblyVisible = false;
                    chunk.stateManager.Visible = false;
                }
                else
                // Weird state
                {
                    Assert.IsFalse(true);
                    chunk.stateManager.RequestState(ChunkState.Remove);
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
                        BlockPos pos = chunk.pos;

                        if (chunk.pos.y==0)
                        {
                            BlockPos localChunkPos = new BlockPos(
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
                        if (chunk.stateManager.IsGenerated)
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
