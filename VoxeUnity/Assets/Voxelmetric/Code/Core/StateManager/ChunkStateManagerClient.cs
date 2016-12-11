using System;
using System.Text;
using Assets.Voxelmetric.Code.Core.StateManager;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core.StateManager
{
    /// <summary>
    /// Handles state changes for chunks from a client's perspective.
    /// This means there chunk geometry rendering and chunk neighbors
    /// need to be taken into account.
    /// </summary>
    public class ChunkStateManagerClient : ChunkStateManager
    {
        //! Says whether or not the chunk is visible
        public bool Visible
        {
            get
            {
                return chunk.GeometryHandler.Batcher.IsEnabled();
            }
            set
            {
                chunk.GeometryHandler.Batcher.Enable(value);
            }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;

        //! Chunk bounds in terms of geometry
        private int m_maxRenderX;
        private int m_minRenderX;
        private int m_maxRenderY;
        private int m_minRenderY;
        private int m_minRenderZ;
        private int m_maxRenderZ;
        private int m_lowestEmptyBlock;

        private bool m_syncEdgeBlocks;

        public ChunkStateManagerClient(Chunk chunk) : base(chunk)
        {
        }

        public override void Init()
        {
            base.Init();

            // Subscribe neighbors
            SubscribeNeighbors(true);
        }

        public override void Reset()
        {
            base.Reset();

            SubscribeNeighbors(false);

            m_stateExternal = ChunkStateExternal.None;

            Visible = false;
            PossiblyVisible = false;

            ResetGeometryBounds();

            m_syncEdgeBlocks = true;
        }

        private void ResetGeometryBounds()
        {
            m_minRenderX = m_minRenderY = m_minRenderZ = Env.ChunkMask;
            m_maxRenderX = m_maxRenderY = m_maxRenderZ = 0;
            m_lowestEmptyBlock = Env.ChunkMask;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("N=");
            sb.Append(m_nextState);
            sb.Append(", P=");
            sb.Append(m_pendingStates);
            sb.Append(", C=");
            sb.Append(m_completedStates);
            return sb.ToString();
        }

        public override void SetMeshBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateVertices);
        }

        public override void SetColliderBuilt()
        {
            m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateCollider);
        }

        public override void Update()
        {
            if (m_stateExternal != ChunkStateExternal.None)
            {
                // Notify everyone listening
                NotifyAll(m_stateExternal);

                m_stateExternal = ChunkStateExternal.None;
            }

            // If removal was requested before we got to generating the chunk at all we can safely mark
            // it as removed right away
            if (m_removalRequested && !m_completedStates.Check(ChunkState.Generate))
            {
                m_completedStates = m_completedStates.Set(ChunkState.Remove);
                return;
            }

            // If there is no pending task, there is nothing for us to do
            ProcessNotifyState();
            if (m_pendingStates==0)
                return;

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            {
                // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                        return;

                    ProcessNotifyState();
                }

                if (m_pendingStates.Check(ChunkState.LoadData) && LoadData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.GenericWork) && PerformGenericWork())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.SaveData) && SaveData())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.Remove) && RemoveChunk())
                    return;

                ProcessNotifyState();
                if (m_pendingStates.Check(ChunkState.BuildCollider) && GenerateCollider())
                    return;

                // In order to save performance, we generate geometry on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(CurrStateGenerateVertices) && GenerateVertices())
                        return;
                }
            }
        }

        private void ProcessNotifyState()
        {
            if (m_nextState == ChunkState.Idle)
                return;

            OnNotified(this, m_nextState);
            m_nextState = ChunkState.Idle;
        }

        public override void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            // Enqueue the request
            m_pendingStates = m_pendingStates.Set(state);
        }

        #region Generic work

        private struct SGenericWorkItem
        {
            public readonly ChunkStateManagerClient Chunk;
            public readonly Action Action;

            public SGenericWorkItem(ChunkStateManagerClient chunk, Action action)
            {
                Chunk = chunk;
                Action = action;
            }
        }

        private static readonly ChunkState CurrStateGenericWork = ChunkState.GenericWork;
        private static readonly ChunkState NextStateGenericWork = ChunkState.Idle;

        private static void OnGenericWork(ref SGenericWorkItem item)
        {
            ChunkStateManagerClient chunk = item.Chunk;
            item.Action();
            OnGenericWorkDone(chunk);
        }

        private static void OnGenericWorkDone(ChunkStateManagerClient chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenericWork);
            chunk.m_nextState = NextStateGenericWork;
            chunk.m_taskRunning = false;
        }

        private bool PerformGenericWork()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateGenericWork);
            m_completedStates = m_completedStates.Reset(CurrStateGenericWork);

            // If there's nothing to do we can skip this state
            if (m_genericWorkItems.Count <= 0)
            {
                OnGenericWorkDone(this);
                m_completedStatesSafe = m_completedStates;
                return false;
            }

            m_completedStatesSafe = m_completedStates;

            // We have work to do
            SGenericWorkItem workItem = new SGenericWorkItem(this, m_genericWorkItems.Dequeue());

            m_taskRunning = true;
            WorkPoolManager.Add(
                new ThreadPoolItem(
                    chunk.ThreadID,
                    arg =>
                    {
                        SGenericWorkItem item = (SGenericWorkItem)arg;
                        OnGenericWork(ref item);
                    },
                    workItem)
                );

            return true;
        }

        public void EnqueueGenericTask(Action action)
        {
            Assert.IsTrue(action != null);
            m_genericWorkItems.Enqueue(action);
            RequestState(ChunkState.GenericWork);
        }

        #endregion

        #region Generate Chunk data

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.LoadData;

        private static void OnGenerateData(ChunkStateManagerClient stateManager)
        {
            Chunk chunk = stateManager.chunk;
            chunk.world.terrainGen.GenerateTerrain(chunk);

            OnGenerateDataDone(stateManager);
        }

        private static void OnGenerateDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateData);
            stateManager.m_nextState = NextStateGenerateData;
            stateManager.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(ChunkStateManagerClient stateManager)
        {
            OnGenerateDataDone(stateManager);
            OnLoadDataDone(stateManager);
        }

        private bool GenerateData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData | CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;

            if (chunk.world.networking.isServer)
            {
                // Let server generate chunk data
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                            OnGenerateData(stateManager);
                        },
                        this)
                    );
            }
            else
            {
                // Client only asks for data
                chunk.world.networking.client.RequestChunk(chunk.pos);
            }

            return true;
        }

        #endregion Generate chunk data

        #region Load chunk data

        private static readonly ChunkState CurrStateLoadData = ChunkState.LoadData;
        private static readonly ChunkState NextStateLoadData = ChunkState.BuildVertices;

        private static void OnLoadData(ChunkStateManagerClient stateManager)
        {
            Serialization.Serialization.LoadChunk(stateManager.chunk);

            OnLoadDataDone(stateManager);
        }

        private static void OnLoadDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateLoadData);
            stateManager.m_nextState = NextStateLoadData;
            stateManager.m_taskRunning = false;
        }

        private bool LoadData()
        {
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateLoadData);
            m_completedStates = m_completedStates.Reset(CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                        OnLoadData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Load chunk data

        #region Save chunk data

        private static readonly ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(ChunkStateManagerClient stateManager)
        {
            Serialization.Serialization.SaveChunk(stateManager.chunk);

            OnSaveDataDone(stateManager);
        }

        private static void OnSaveDataDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_stateExternal = ChunkStateExternal.Saved;
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateSaveData);
            stateManager.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated and data finalized
            if (!m_completedStates.Check(ChunkState.Generate) || !m_completedStates.Check(ChunkState.LoadData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateSaveData);
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);
            m_completedStatesSafe = m_completedStates;

            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        ChunkStateManagerClient stateManager = (ChunkStateManagerClient)arg;
                        OnSaveData(stateManager);
                    },
                    this)
                );

            return true;
        }

        #endregion Save chunk data

        private void AdjustMinMaxRenderBounds(int x, int y, int z)
        {
            ushort type = chunk.blocks.Get(new Vector3Int(x, y, z)).Type;
            if (type != BlockProvider.AirType)
            {
                if (x < m_minRenderX)
                    m_minRenderX = x;
                if (y < m_minRenderY)
                    m_minRenderY = y;
                if (z < m_minRenderZ)
                    m_minRenderZ = z;

                if (x > m_maxRenderX)
                    m_maxRenderX = x;
                if (y > m_maxRenderY)
                    m_maxRenderY = y;
                if (z > m_maxRenderZ)
                    m_maxRenderZ = z;
            }
            else if (y < m_lowestEmptyBlock)
                m_lowestEmptyBlock = y;
        }

        private void CalculateGeometryBounds()
        {
            ResetGeometryBounds();

            for (int y = Env.ChunkMask; y >= 0; y--)
            {
                for (int z = 0; z <= Env.ChunkMask; z++)
                {
                    for (int x = 0; x <= Env.ChunkMask; x++)
                    {
                        AdjustMinMaxRenderBounds(x, y, z);
                    }
                }
            }

            // This is an optimization - if this chunk is flat than there's no need to consider it as a whole.
            // Its' top part is sufficient enough. However, we never want this value be smaller than chunk's
            // lowest solid part.
            // E.g. a sphere floating above the ground would be considered from its topmost solid block to
            // the ground without this. With this check, the lowest part above ground will be taken as minimum
            // render value.
            m_minRenderY = Mathf.Max(m_lowestEmptyBlock - 1, m_minRenderY);
            m_minRenderY = Mathf.Max(m_minRenderY, 0);

            // Consume info about block having been modified
            chunk.blocks.contentsInvalidated = false;
        }

        private bool SynchronizeNeighbors()
        {
            // 6 neighbors are necessary
            if (ListenerCount != 6)
                return false;

            // All neighbors have to have their data loaded
            foreach (var chunkEvent in Listeners)
            {
                var stateManager = (ChunkStateManagerClient)chunkEvent;
                if (!stateManager.m_completedStates.Check(ChunkState.LoadData))
                    return false;
            }

            return true;
        }

        private void SynchronizeEdges()
        {
            // It is only necessary to perform the sychronization once when data is generated.
            // All subsequend changes of blocks are automatically synchronized inside ChunkBlocks
            if (!m_syncEdgeBlocks)
                return;
            m_syncEdgeBlocks = false;

            // Search for neighbors we are vertically aligned with
            for (int i = 0; i<Listeners.Length; i++)
            {
                var chunkEvent = Listeners[i];
                var stateManager = (ChunkStateManagerClient)chunkEvent;
                Chunk neighborChunk = stateManager.chunk;

                // Sync vertical neighbors
                if (neighborChunk.pos.x==chunk.pos.x &&
                    neighborChunk.pos.z==chunk.pos.z)
                {
                    // Copy the bottom layer of a neighbor chunk to the top layer of ours
                    if (neighborChunk.pos.y>chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, 0, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkSize, -1);
                        chunk.blocks.Copy(neighborChunk.blocks, srcIndex, dstIndex, Env.ChunkSizeWithPaddingPow2);
                    }
                    // Copy the top layer of a neighbor chunk to the bottom layer of ours
                    else // if (neighborChunk.pos.y < chunk.pos.y)
                    {
                        int srcIndex = Helpers.GetChunkIndex1DFrom3D(-1, Env.ChunkMask, -1);
                        int dstIndex = Helpers.GetChunkIndex1DFrom3D(-1, -1, -1);
                        chunk.blocks.Copy(neighborChunk.blocks, srcIndex, dstIndex, Env.ChunkSizeWithPaddingPow2);
                    }
                }

                // Sync front and back neighbors
                if (neighborChunk.pos.x==chunk.pos.x &&
                    neighborChunk.pos.y==chunk.pos.y)
                {
                    // Copy the front layer of a neighbor chunk to the back layer of ours
                    if (neighborChunk.pos.z>chunk.pos.z)
                    {
                        for (int y = -1; y<Env.ChunkSizePlusPadding; y++)
                        {
                            for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                            {
                                BlockData data = neighborChunk.blocks.Get(new Vector3Int(x, y, 0));
                                chunk.blocks.Set(new Vector3Int(x, y, Env.ChunkSize), data);
                            }
                        }
                    }
                    // Copy the top back layer of a neighbor chunk to the front layer of ours
                    else // if (neighborChunk.pos.z < chunk.pos.z)
                    {
                        for (int y = -1; y<Env.ChunkSizePlusPadding; y++)
                        {
                            for (int x = -1; x<Env.ChunkSizePlusPadding; x++)
                            {
                                BlockData data = neighborChunk.blocks.Get(new Vector3Int(x, y, Env.ChunkMask));
                                chunk.blocks.Set(new Vector3Int(x, y, -1), data);
                            }
                        }
                    }
                }

                // Sync right and left neighbors
                if (neighborChunk.pos.y==chunk.pos.y &&
                    neighborChunk.pos.z==chunk.pos.z)
                {
                    // Copy the right layer of a neighbor chunk to the left layer of ours
                    if (neighborChunk.pos.x>chunk.pos.x)
                    {
                        for (int y = -1; y<Env.ChunkSizePlusPadding; y++)
                        {
                            for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                            {
                                BlockData data = neighborChunk.blocks.Get(new Vector3Int(0, y, z));
                                chunk.blocks.Set(new Vector3Int(Env.ChunkSize, y, z), data);
                            }
                        }
                    }
                    // Copy the left layer of a neighbor chunk to the right layer of ours
                    else // if (neighborChunk.pos.x < chunk.pos.x)
                    {
                        for (int y = -1; y<Env.ChunkSizePlusPadding; y++)
                        {
                            for (int z = -1; z<Env.ChunkSizePlusPadding; z++)
                            {
                                BlockData data = neighborChunk.blocks.Get(new Vector3Int(Env.ChunkMask, y, z));
                                chunk.blocks.Set(new Vector3Int(-1, y, z), data);
                            }
                        }
                    }
                }
            }
        }

        private bool SynchronizeChunk()
        {
            // 6 neighbors are necessary to be loaded
            if (!SynchronizeNeighbors())
                return false;

            // Synchronize edge data of chunks
            SynchronizeEdges();

            // We need to calculate our chunk's bounds if it was invalidated
            if (chunk.blocks.contentsInvalidated && chunk.blocks.NonEmptyBlocks>0)
            {
                EnqueueGenericTask(CalculateGeometryBounds);
                return false;
            }

            return true;
        }

        #region Generate collider

        private struct SGenerateColliderWorkItem
        {
            public readonly ChunkStateManagerClient StateManager;
            public readonly int MinX;
            public readonly int MaxX;
            public readonly int MinY;
            public readonly int MaxY;
            public readonly int MinZ;
            public readonly int MaxZ;

            public SGenerateColliderWorkItem(ChunkStateManagerClient stateManager, int minX, int maxX, int minY, int maxY, int minZ, int maxZ)
            {
                StateManager = stateManager;
                MinX = minX;
                MaxX = maxX;
                MinY = minY;
                MaxY = maxY;
                MinZ = minZ;
                MaxZ = maxZ;
            }
        }

        private static readonly ChunkState CurrStateGenerateCollider = ChunkState.BuildCollider;

        private static void OnGenerateCollider(ref SGenerateColliderWorkItem item)
        {
            ChunkStateManagerClient stateManager = item.StateManager;
            stateManager.chunk.ChunkColliderGeometryHandler.Build(item.MinX, item.MaxX, item.MinY, item.MaxY, item.MinZ, item.MaxZ);
            OnGenerateColliderDone(stateManager);
        }

        private static void OnGenerateColliderDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateCollider);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's collision geometry
        /// </summary>
        private bool GenerateCollider()
        {
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            if (!SynchronizeChunk())
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateCollider);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateCollider);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var workItem = new SGenerateColliderWorkItem(
                    this,
                    m_minRenderX, m_maxRenderX,
                    m_minRenderY, m_maxRenderY,
                    m_minRenderZ, m_maxRenderZ
                    );

                m_taskRunning = true;
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenerateColliderWorkItem item = (SGenerateColliderWorkItem)arg;
                            OnGenerateCollider(ref item);
                        },
                        workItem)
                    );

                return true;
            }

            OnGenerateColliderDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Generate vertices

        private struct SGenerateVerticesWorkItem
        {
            public readonly ChunkStateManagerClient StateManager;

            public SGenerateVerticesWorkItem(ChunkStateManagerClient stateManager)
            {
                StateManager = stateManager;
            }
        }

        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices | ChunkState.BuildVerticesNow;

        private static void OnGenerateVerices(ref SGenerateVerticesWorkItem item)
        {
            ChunkStateManagerClient stateManager = item.StateManager;
            stateManager.chunk.GeometryHandler.Build(0, Env.ChunkMask, 0, Env.ChunkMask, 0, Env.ChunkMask);
            OnGenerateVerticesDone(stateManager);
        }

        private static void OnGenerateVerticesDone(ChunkStateManagerClient stateManager)
        {
            stateManager.m_completedStates = stateManager.m_completedStates.Set(CurrStateGenerateVertices);
            stateManager.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool GenerateVertices()
        {
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            if (!SynchronizeChunk())
                return true;

            bool priority = m_pendingStates.Check(ChunkState.BuildVerticesNow);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateVertices);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateVertices);
            m_completedStatesSafe = m_completedStates;

            if (chunk.blocks.NonEmptyBlocks > 0)
            {
                var workItem = new SGenerateVerticesWorkItem(this);

                m_taskRunning = true;
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        chunk.ThreadID,
                        arg =>
                        {
                            SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                            OnGenerateVerices(ref item);
                        },
                        workItem,
                        priority ? Globals.Watch.ElapsedTicks : long.MaxValue)
                    );

                return true;
            }

            OnGenerateVerticesDone(this);
            return false;
        }

        #endregion Generate vertices

        #region Remove chunk

        private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

        private bool RemoveChunk()
        {
            // If chunk was generated we need to wait for other states with higher priority to finish first
            if (m_completedStates.Check(ChunkState.Generate))
            {
                if (!m_completedStates.Check(
                    // Wait until chunk is loaded
                    ChunkState.LoadData|
                    // Wait until chunk data is stored
                    ChunkState.SaveData
                    ))
                    return true;

                m_pendingStates = m_pendingStates.Reset(CurrStateRemoveChunk);
            }

            m_completedStates = m_completedStates.Set(CurrStateRemoveChunk);
            return true;
        }

        #endregion Remove chunk

        private void SubscribeNeighbors(bool subscribe)
        {
            Vector3Int pos = chunk.pos;
            SubscribeTwoNeighbors(new Vector3Int(pos.x + Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x - Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y + Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y - Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y, pos.z + Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(new Vector3Int(pos.x, pos.y, pos.z - Env.ChunkSize), subscribe);
        }

        private void SubscribeTwoNeighbors(Vector3Int neighborPos, bool subscribe)
        {
            Chunk neighbor = chunk.world.chunks.Get(neighborPos);
            if (neighbor != null)
            {
                ChunkStateManagerClient stateManager = (ChunkStateManagerClient)neighbor.stateManager;
                // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
                stateManager.Subscribe(this, ChunkState.Idle, subscribe);
                Subscribe(stateManager, ChunkState.Idle, subscribe);
            }
        }
    }
}
