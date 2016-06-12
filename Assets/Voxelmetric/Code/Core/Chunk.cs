using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Common.Threading;
using Voxelmetric.Code.Common.Threading.Managers;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core
{
    public sealed class Chunk: ChunkEvent
    {
        private static int s_id = 0;

        public World world { get; private set; }
        public BlockPos pos { get; private set; }
        public LocalPools pools { get; private set; }

        public ChunkBlocks blocks { get; private set; }
        public ChunkLogic logic { get; private set; }
        public ChunkRender render { get; private set; }

        //! Bounding box in world coordinates
        public Bounds WorldBounds { get; private set; }

        //! Specifies whether there's a task running on this Chunk
        private bool m_taskRunning;
        private object m_lock;

        //! Next state after currently finished state
        private ChunkState m_notifyStates;
        //! Tasks waiting to be executed
        private ChunkState m_pendingStates;
        //! States to be refreshed
        private ChunkState m_refreshStates;
        //! Tasks already executed
        private ChunkState m_completedStates;
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        private bool m_removalRequested;

        //! State to notify external listeners about
        private ChunkStateExternal m_stateExternal;

        //! A list of generic tasks a Chunk has to perform
        private readonly List<Action> m_genericWorkItems = new List<Action>();
        //! Number of generic tasks waiting to be finished
        private int m_genericWorkItemsLeftToProcess;

        //! ThreadID associated with this chunk. Used when working with object pools in MT environment. Resources
        //! need to be release where they were allocated. Thanks to this, associated containers could be made lock-free
        public int ThreadID { get; private set; }

        //! Says whether or not the chunk is visible
        public bool Visible
        {
            get { return render.batcher.IsVisible(); }
            set { render.batcher.SetVisible(value); }
        }
        //! Says whether or not building of geometry can be triggered
        public bool PossiblyVisible { get; set; }

        public static Chunk CreateChunk(World world, BlockPos pos)
        {
            Chunk chunk = Globals.MemPools.ChunkPool.Pop();
            chunk.Init(world, pos);
            return chunk;
        }

        public static void RemoveChunk(Chunk chunk)
        {
            chunk.Reset();
            chunk.world = null;
            Globals.MemPools.ChunkPool.Push(chunk);
        }

        public Chunk()
        {
            // Associate Chunk with a certain thread and make use of its memory pool
            // This is necessary in order to have lock-free caches
            ThreadID = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
            pools = Globals.WorkPool.GetPool(ThreadID);

            render = new ChunkRender(this);
            blocks = new ChunkBlocks(this);
            logic = new ChunkLogic(this);

            m_lock = new object();
        }

        private void Init(World world, BlockPos pos)
        {
            this.world = world;
            this.pos = pos;

            const int size = Env.ChunkSize;
            WorldBounds = new Bounds(
                new Vector3(pos.x+size/2, pos.y+size/2, pos.z+size/2),
                new Vector3(size, size, size)
                );

            Reset();

            // Subscribe neighbors
            SubscribeNeighbors(true);
            // Request this chunk to be generated
            OnNotified(this, ChunkState.Generate);
        }

        private void Reset()
        {
            SubscribeNeighbors(false);

            m_notifyStates = m_notifyStates.Reset();
            m_pendingStates = m_pendingStates.Reset();
            m_refreshStates = m_refreshStates.Reset();
            m_completedStates = m_completedStates.Reset();
            m_removalRequested = false;

            m_stateExternal = ChunkStateExternal.None;

            m_genericWorkItems.Clear();
            m_genericWorkItemsLeftToProcess = 0;

            blocks.Reset();
            logic.Reset();
            render.Reset();

            m_taskRunning = false;

            Visible = false;
            PossiblyVisible = false;

            Clear();
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append(pos);
            sb.Append(", N=");
            sb.Append(m_notifyStates);
            sb.Append(", P=");
            sb.Append(m_pendingStates);
            sb.Append(", R=");
            sb.Append(m_refreshStates);
            sb.Append(", C=");
            sb.Append(m_completedStates);
            sb.Append(", blocks=");
            sb.Append(blocks);
            sb.Append(", logic=");
            sb.Append(logic);
            sb.Append(", render=");
            sb.Append(render);
            return sb.ToString();
        }
        
        private void UpdateLogic()
        {
            // Do not update chunk until it has all its data prepared
            if (!m_completedStates.Check(ChunkState.Generate) ||
                !m_completedStates.Check(ChunkState.LoadData)
                )
                return;

            logic.TimedUpdated();
        }
        
        public void RequestBuildVertices()
        {
            RefreshState(ChunkState.BuildVertices);
        }

        public void RequestSaveData()
        {
            RefreshState(ChunkState.SaveData);
        }

        public void RequestRemoval()
        {
            if (m_removalRequested)
                return;
            m_removalRequested = true;

            RefreshState(ChunkState.SaveData);
            OnNotified(this, ChunkState.Remove);
        }

        public void UpdateChunk()
        {
            ProcessPendingTasks();
            UpdateLogic();

            // Build chunk mesh
            if (m_completedStates.Check(ChunkState.BuildVertices))
            {
                m_completedStates = m_completedStates.Reset(ChunkState.BuildVertices);
                render.BuildMesh();
            }
        }
        private void ProcessPendingTasks()
        {
            lock (m_lock)
            {
                // We are not allowed to check anything as long as there is a task still running
                if (IsExecutingTask_Internal())
                    return;

                // Once this Chunk is marked as finished we stop caring about everything else
                if (IsFinished_Internal())
                    return;
            }

            if (m_stateExternal!=ChunkStateExternal.None)
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

            // Go from the least important bit to most important one. If a given bit it set
            // we execute the task tied with it
            {
                // In order to save performance, we generate chunk data on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(ChunkState.Generate) && GenerateData())
                        return;
                }

                ProcessNotifyState();
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

                // In order to save performance, we generate geometry on-demand - when the chunk can be seen
                if (PossiblyVisible)
                {
                    ProcessNotifyState();
                    if (m_pendingStates.Check(ChunkState.BuildVertices) && GenerateVertices())
                        return;
                }
            }
        }

        private void ProcessNotifyState()
        {
            if (m_notifyStates==ChunkState.Idle)
                return;
            
            OnNotified(this, m_notifyStates);
            m_notifyStates = ChunkState.Idle;
        }

        private bool IsFinished_Internal()
        {
            return m_completedStates.Check(ChunkState.Remove);
        }

        public bool IsFinished
        {
            get { lock (m_lock) return IsFinished_Internal(); }
        }

        public bool IsSavePossible
        {
            get { lock (m_lock) { return !m_removalRequested && m_completedStates.Check(ChunkState.Generate|ChunkState.LoadData); } }
        }
        
        private bool IsExecutingTask_Internal()
        {
            return m_taskRunning;
        }

        public bool IsExecutingTask
        {
            get { lock (m_lock) return IsExecutingTask_Internal(); }
        }

        public override void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            // Enqueue the request
            m_pendingStates = m_pendingStates.Set(state);
        }

        private void RefreshState(ChunkState state)
        {
            m_refreshStates = m_refreshStates.Set(state);
            m_pendingStates = m_pendingStates.Set(state);
        }

        #region Generic work

        private struct SGenericWorkItem
        {
            public readonly Chunk Chunk;
            public readonly Action Action;

            public SGenericWorkItem(Chunk chunk, Action action)
            {
                Chunk = chunk;
                Action = action;
            }
        }

        private static readonly ChunkState CurrStateGenericWork = ChunkState.GenericWork;
        private static readonly ChunkState NextStateGenericWork = ChunkState.Idle;

        private static void OnGenericWork(ref SGenericWorkItem item)
        {
            Chunk chunk = item.Chunk;

            // Perform the action
            item.Action();

            int cnt = Interlocked.Decrement(ref chunk.m_genericWorkItemsLeftToProcess);
            if (cnt<=0)
            {
                // Something is very wrong if we go below zero
                Assert.IsTrue(cnt==0);

                // All generic work is done
                lock (chunk.m_lock)
                {
                    OnGenericWorkDone(chunk);
                }
            }
        }

        private static void OnGenericWorkDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenericWork);
            chunk.m_notifyStates = NextStateGenericWork;
            chunk.m_taskRunning = false;
        }

        private bool PerformGenericWork()
        {
            // When we get here we expect all generic tasks to be processed
            Assert.IsTrue(Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0)==0);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenericWork);

            // Nothing here for us to do if the Chunk was not changed
            if (m_completedStates.Check(CurrStateGenericWork) && !m_refreshStates.Check(CurrStateGenericWork))
            {
                m_genericWorkItemsLeftToProcess = 0;
                OnGenericWorkDone(this);
                return false;
            }

            m_refreshStates = m_refreshStates.Reset(CurrStateGenericWork);
            m_completedStates = m_completedStates.Reset(CurrStateGenericWork);

            // If there's nothing to do we can skip this state
            if (m_genericWorkItems.Count<=0)
            {
                m_genericWorkItemsLeftToProcess = 0;
                OnGenericWorkDone(this);
                return false;
            }

            m_taskRunning = true;
            m_genericWorkItemsLeftToProcess = m_genericWorkItems.Count;

            for (int i = 0; i<m_genericWorkItems.Count; i++)
            {
                SGenericWorkItem workItem = new SGenericWorkItem(this, m_genericWorkItems[i]);

                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        ThreadID,
                        arg =>
                        {
                            SGenericWorkItem item = (SGenericWorkItem)arg;
                            OnGenericWork(ref item);
                        },
                        workItem)
                    );
            }
            m_genericWorkItems.Clear();

            return true;
        }

        public void EnqueueGenericTask(Action action)
        {
            Assert.IsTrue(action!=null);
            m_genericWorkItems.Add(action);
            RefreshState(ChunkState.GenericWork);
        }

        #endregion

        #region Generate Chunk data

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.LoadData;

        private static void OnGenerateData(Chunk chunk)
        {
            chunk.world.terrainGen.GenerateTerrainForChunk(chunk);

            lock (chunk.m_lock)
            {
                OnGenerateDataDone(chunk);
            }
        }

        private static void OnGenerateDataDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenerateData);
            chunk.m_notifyStates = NextStateGenerateData;
            chunk.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(Chunk chunk)
        {
            lock (chunk.m_lock)
            {
                OnGenerateDataDone(chunk);
                OnLoadDataDone(chunk);
            }
        }

        private bool GenerateData()
        {
            if (m_completedStates.Check(CurrStateGenerateData))
            {
                m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);

                OnGenerateDataDone(this);
                return false;
            }
            
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_refreshStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData);

            m_taskRunning = true;

            if (world.networking.isServer)
            {
                // Let server generate chunk data
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        ThreadID,
                        arg =>
                        {
                            Chunk chunk = (Chunk)arg;
                            OnGenerateData(chunk);
                        },
                        this)
                    );
            }
            else
            {
                // Client only asks for data
                world.networking.client.RequestChunk(pos);
            }

            return true;
        }

        #endregion Generate chunk data

        #region Load chunk data

        private static readonly ChunkState CurrStateLoadData = ChunkState.LoadData;
        private static readonly ChunkState NextStateLoadData = ChunkState.BuildVertices;

        private static void OnLoadData(Chunk chunk)
        {
            Serialization.Serialization.LoadChunk(chunk);

            lock (chunk.m_lock)
            {
                OnLoadDataDone(chunk);
            }
        }

        private static void OnLoadDataDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateLoadData);
            chunk.m_notifyStates = NextStateLoadData;
            chunk.m_taskRunning = false;
        }

        private bool LoadData()
        {
            /*Assert.IsTrue(
                m_completedStates.Check(ChunkState.Generate),
                string.Format(
                    "[{0},{1},{2}] - LoadData set sooner than Generate completed. Pending:{3}, Completed:{4}", pos.x,
                    pos.y, pos.z, m_pendingStates, m_completedStates)
                );*/
            if (!m_completedStates.Check(ChunkState.Generate))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateLoadData);

            // Nothing here for us to do if the Chunk was not changed
            if (m_completedStates.Check(CurrStateLoadData))
            {
                m_refreshStates = m_refreshStates.Reset(CurrStateLoadData);
                OnLoadDataDone(this);
                return false;
            }

            m_refreshStates = m_refreshStates.Reset(CurrStateLoadData);
            m_completedStates = m_completedStates.Reset(CurrStateLoadData);
            
            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        Chunk chunk = (Chunk)arg;
                        OnLoadData(chunk);
                    },
                    this)
                );

            return true;
        }

        #endregion Load chunk data

        #region Save chunk data
        
        private static readonly ChunkState CurrStateSaveData = ChunkState.SaveData;

        private static void OnSaveData(Chunk chunk)
        {
            Serialization.Serialization.SaveChunk(chunk);

            lock (chunk.m_lock)
            {
                OnSaveDataDone(chunk);
            }
        }

        private static void OnSaveDataDone(Chunk chunk)
        {
            chunk.m_stateExternal = ChunkStateExternal.Saved;
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateSaveData);
            chunk.m_taskRunning = false;
        }

        private bool SaveData()
        {
            // We need to wait until chunk is generated and data finalized
            if (!m_completedStates.Check(ChunkState.Generate) || !m_completedStates.Check(ChunkState.LoadData))
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateSaveData);

            // Nothing here for us to do if the Chunk was not changed since the last serialization
            if (m_completedStates.Check(CurrStateSaveData) && !m_refreshStates.Check(CurrStateSaveData))
            {
                OnSaveDataDone(this);
                return false;
            }

            m_refreshStates = m_refreshStates.Reset(CurrStateSaveData);
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);

            m_taskRunning = true;
            IOPoolManager.Add(
                new TaskPoolItem(
                    arg =>
                    {
                        Chunk chunk = (Chunk)arg;
                        OnSaveData(chunk);
                    },
                    this)
                );

            return true;
        }

        #endregion Save chunk data

        private bool SynchronizeChunk()
        {
            // 6 neighbors are necessary
            if (ListenerCount!=6)
                return false;

            // All neighbors have to have their data loaded
            foreach (var chunkEvent in Listeners)
            {
                var chunk = (Chunk)chunkEvent;
                if (!chunk.m_completedStates.Check(ChunkState.LoadData))
                    return false;
            }

            return true;
        }

        #region Generate vertices

        private struct SGenerateVerticesWorkItem
        {
            public readonly Chunk Chunk;

            public SGenerateVerticesWorkItem(Chunk chunk)
            {
                Chunk = chunk;
            }
        }

        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices;

        private static void OnGenerateVerices(Chunk chunk)
        {
            chunk.render.BuildMeshData();

            lock (chunk.m_lock)
            {
                OnGenerateVerticesDone(chunk);
            }
        }

        private static void OnGenerateVerticesDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenerateVertices);
            chunk.m_taskRunning = false;
        }

        /// <summary>
        ///     Build this chunk's geometry
        /// </summary>
        private bool GenerateVertices()
        {
            /*Assert.IsTrue(
            m_completedTasks.Check(ChunkState.LoadData),
            string.Format("[{0},{1},{2}] - GenerateVertices set sooner than LoadData completed. Pending:{3}, Completed:{4}", Pos.X, Pos.Y, Pos.Z, m_pendingTasks, m_completedTasks)
            );*/
            if (!m_completedStates.Check(ChunkState.LoadData))
                return true;

            if (!SynchronizeChunk())
                return true;

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateVertices);

            // Nothing here for us to do if the chunk was not changed since the last time geometry was built
            if (m_completedStates.Check(CurrStateGenerateVertices) && !m_refreshStates.Check(CurrStateGenerateVertices))
            {
                OnGenerateVerticesDone(this);
                return false;
            }

            m_refreshStates = m_refreshStates.Reset(CurrStateGenerateVertices);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateVertices);

            /*if (NonEmptyBlocks > 0)
            {*/
                var workItem = new SGenerateVerticesWorkItem(this);

                m_taskRunning = true;
                WorkPoolManager.Add(
                    new ThreadPoolItem(
                        ThreadID,
                        arg =>
                        {
                            SGenerateVerticesWorkItem item = (SGenerateVerticesWorkItem)arg;
                            OnGenerateVerices(item.Chunk);
                        },
                        workItem)
                    );
                /*}
            else
            {
                OnGenerateVerticesDone(this);
            }*/

            return true;
        }

        #endregion Generate vertices

        #region Remove chunk

        private static readonly ChunkState CurrStateRemoveChunk = ChunkState.Remove;

        private bool RemoveChunk()
        {
            // Wait until all generic tasks are processed
            if (Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0)!=0)
            {
                Assert.IsTrue(false);
                return true;
            }

            // If chunk was generated we need to wait for other states with higher priority to finish first
            if (m_completedStates.Check(ChunkState.Generate))
            {
                // LoadData need to finish first
                if (!m_completedStates.Check(ChunkState.LoadData))
                    return true;

                // Wait for serialization to finish as well
                if (!m_completedStates.Check(ChunkState.SaveData))
                    return true;

                m_pendingStates = m_pendingStates.Reset(CurrStateRemoveChunk);
            }

            m_refreshStates = m_refreshStates.Reset(CurrStateRemoveChunk);
            m_completedStates = m_completedStates.Set(CurrStateRemoveChunk);
            return true;
        }

        #endregion Remove chunk
        
        private void SubscribeNeighbors(bool subscribe)
        {
            SubscribeTwoNeighbors(new BlockPos(pos.x + Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x - Env.ChunkSize, pos.y, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y + Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y - Env.ChunkSize, pos.z), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y, pos.z + Env.ChunkSize), subscribe);
            SubscribeTwoNeighbors(new BlockPos(pos.x, pos.y, pos.z - Env.ChunkSize), subscribe);
        }

        private void SubscribeTwoNeighbors(BlockPos neighborPos, bool subscribe)
        {
            Chunk neighbor = world.chunks.Get(neighborPos);
            if (neighbor != null)
            {
                // Subscribe with each other. Passing Idle as event - it is ignored in this case anyway
                neighbor.Subscribe(this, ChunkState.Idle, subscribe);
                Subscribe(neighbor, ChunkState.Idle, subscribe);
            }
        }
    }
}