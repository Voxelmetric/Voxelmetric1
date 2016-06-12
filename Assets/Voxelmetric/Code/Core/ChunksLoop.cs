using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Math;

namespace Voxelmetric.Code.Core
{
    /// <summary>
    /// This class runs constantly running generation jobs for chunks. When chunks are added to one of the
    /// generation stages (chunk.generationStage) it should also be added to a list here and this 
    /// class will work through every list running the job for the relevant stage and pushing it to the
    /// next stage. Chunks can be added and forgotten because they will work their way to fully functioning
    /// chunks by the end.
    /// 
    /// Use ChunksInProgress to check the number of chunks in queue to be generated before adding a new one.
    /// There's no point in piling up the queue, better to wait, then add more.
    /// </summary>
    public class ChunksLoop
    {
        private World world;
        private readonly List<Chunk> markedForDeletion = new List<Chunk>();
        private Plane[] m_cameraPlanes = new Plane[6];

        public ChunksLoop(World world)
        {
            this.world = world;
        }

        public void Stop()
        {
        }

        public void Update()
        {
            // Recalculate camera frustum planes
            Geometry.CalculateFrustumPlanes(Camera.main, ref m_cameraPlanes);

            // Process chunks
            UpdateChunks();
        }

        // The ugliest thing... Until I come with an idea of how to efficiently detect whether a chunk is partialy
        // inside camera frustum, all chunks are going to be marked as potentially visible on the first run
        private bool m_firstRun = true;

        private void UpdateChunks()
        {
            foreach (Chunk chunk in world.chunks.chunkCollection)
            {
                // Chunks marked as finished should be removed from the world
                if (chunk.IsFinished)
                {
                    markedForDeletion.Add(chunk);
                    continue;
                }

                // Update visibility information
                if (world.UseFrustumCulling)
                {
                    bool isInsideFrustum = IsChunkInViewFrustum(chunk) || m_firstRun;
                    chunk.Visible = isInsideFrustum;
                    chunk.PossiblyVisible = isInsideFrustum;
                }
                else
                {
                    chunk.Visible = true;
                    chunk.PossiblyVisible = true;
                }

                // Process the chunk
                chunk.UpdateChunk();
            }

            m_firstRun = false;

            for (int i = 0; i<markedForDeletion.Count; i++)
            {
                Chunk chunk = markedForDeletion[i];
                world.chunks.Remove(chunk);
            }
            markedForDeletion.Clear();
        }

        public bool IsChunkInViewFrustum(Chunk chunk)
        {
            // Check if the chunk lies within camera planes
            return !world.UseFrustumCulling || GeometryUtility.TestPlanesAABB(m_cameraPlanes, chunk.WorldBounds);
        }
    }
}