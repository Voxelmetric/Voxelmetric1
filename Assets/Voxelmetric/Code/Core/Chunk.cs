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
        private volatile bool m_taskRunning;

        //! Next state after currently finished state
        private ChunkState m_nextState;
        //! States waiting to be processed
        private ChunkState m_pendingStates;
        //! Tasks already executed
        private ChunkState m_completedStates;
        //! Just like m_completedStates, but it is synchronized on the main thread once a check for m_taskRunning is passed
        private ChunkState m_completedStatesSafe;
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

            blocks.Init();

            // Subscribe neighbors
            SubscribeNeighbors(true);
            // Request this chunk to be generated
            OnNotified(this, ChunkState.Generate);
        }

        private void Reset()
        {
            SubscribeNeighbors(false);

            m_nextState = m_nextState.Reset();
            m_pendingStates = m_pendingStates.Reset();
            m_completedStates = m_completedStates.Reset();
            m_completedStatesSafe = m_completedStates;
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
            sb.Append(m_nextState);
            sb.Append(", P=");
            sb.Append(m_pendingStates);
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
        
        public void RequestBuildVertices()
        {
            RequestState(ChunkState.BuildVertices);
        }

        public void RequestBuildVerticesNow()
        {
            RequestState(ChunkState.BuildVerticesNow);
        }

        public void RequestSaveData()
        {
            RequestState(ChunkState.SaveData);
        }

        public void RequestRemoval()
        {
            if (m_removalRequested)
                return;
            m_removalRequested = true;

            RequestState(ChunkState.SaveData);
            OnNotified(this, ChunkState.Remove);
        }

        public void UpdateChunk()
        {
            // Do not do any processing as long as there is any task still running
            // Note that this check is not thread-safe because this value can be changed from a different thread. However,
            // we do not care. The worst thing that can happen is that we read a value which is one frame old. So be it.
            // Thanks to being this relaxed approach we do not need any synchronization primitives at all.
            if (IsExecutingTask)
                return;

            // Synchronize the value with what we have on a different thread. It would be no big deal not having this at
            // all. However, it is technically more correct.
            m_completedStatesSafe = m_completedStates;

            // Once this Chunk is marked as finished we ignore any further requests and won't perform any updates
            if (IsFinished)
                return;

            // Do not update our chunk until it has all its data prepared
            if (m_completedStatesSafe.Check(ChunkState.LoadData))
            {
                logic.Update();
                blocks.Update();
            }

            // Build chunk mesh if necessary
            if (m_completedStatesSafe.Check(CurrStateGenerateVertices))
            {
                m_completedStates = m_completedStatesSafe = m_completedStates.Reset(CurrStateGenerateVertices);
                render.BuildMesh();
            }

            // Process chunk tasks
            ProcessPendingTasks();
        }
        private void ProcessPendingTasks()
        {
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
                    if (m_pendingStates.Check(CurrStateGenerateVertices) && GenerateVertices())
                        return;
                }
            }
        }

        private void ProcessNotifyState()
        {
            if (m_nextState==ChunkState.Idle)
                return;
            
            OnNotified(this, m_nextState);
            m_nextState = ChunkState.Idle;
        }
        
        public bool IsFinished
        {
            get { return m_completedStatesSafe.Check(ChunkState.Remove); }
        }

        public bool IsGenerated
        {
            get { return m_completedStatesSafe.Check(ChunkState.Generate); }
        }

        public bool IsSavePossible
        {
            get { return !m_removalRequested && m_completedStatesSafe.Check(ChunkState.Generate|ChunkState.LoadData); }
        }

        public bool IsExecutingTask
        {
            get { return m_taskRunning; }
        }

        public override void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            // Enqueue the request
            m_pendingStates = m_pendingStates.Set(state);
        }

        private void RequestState(ChunkState state)
        {
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
                OnGenericWorkDone(chunk);
            }
        }

        private static void OnGenericWorkDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenericWork);
            chunk.m_nextState = NextStateGenericWork;
            chunk.m_taskRunning = false;
        }

        private bool PerformGenericWork()
        {
            // When we get here we expect all generic tasks to be processed
            Assert.IsTrue(Interlocked.CompareExchange(ref m_genericWorkItemsLeftToProcess, 0, 0)==0);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenericWork);
            m_completedStates = m_completedStates.Reset(CurrStateGenericWork);
            m_completedStatesSafe = m_completedStates;

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
            RequestState(ChunkState.GenericWork);
        }

        #endregion

        #region Generate Chunk data

        private static readonly ChunkState CurrStateGenerateData = ChunkState.Generate;
        private static readonly ChunkState NextStateGenerateData = ChunkState.LoadData;

        private static void OnGenerateData(Chunk chunk)
        {
            chunk.world.terrainGen.GenerateTerrainForChunk(chunk);

            OnGenerateDataDone(chunk);
        }

        private static void OnGenerateDataDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateGenerateData);
            chunk.m_nextState = NextStateGenerateData;
            chunk.m_taskRunning = false;
        }

        public static void OnGenerateDataOverNetworkDone(Chunk chunk)
        {
            OnGenerateDataDone(chunk);
            OnLoadDataDone(chunk);
        }

        private bool GenerateData()
        {
            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateData);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateData|CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

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

            OnLoadDataDone(chunk);
        }

        private static void OnLoadDataDone(Chunk chunk)
        {
            chunk.m_completedStates = chunk.m_completedStates.Set(CurrStateLoadData);
            chunk.m_nextState = NextStateLoadData;
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
            m_completedStates = m_completedStates.Reset(CurrStateLoadData);
            m_completedStatesSafe = m_completedStates;

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

            OnSaveDataDone(chunk);
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
            m_completedStates = m_completedStates.Reset(CurrStateSaveData);
            m_completedStatesSafe = m_completedStates;

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

        private static readonly ChunkState CurrStateGenerateVertices = ChunkState.BuildVertices | ChunkState.BuildVerticesNow;

        private static void OnGenerateVerices(Chunk chunk)
        {
            chunk.render.BuildMeshData();

            OnGenerateVerticesDone(chunk);
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

            bool priority = m_pendingStates.Check(ChunkState.BuildVerticesNow);

            m_pendingStates = m_pendingStates.Reset(CurrStateGenerateVertices);
            m_completedStates = m_completedStates.Reset(CurrStateGenerateVertices);
            m_completedStatesSafe = m_completedStates;

            if (blocks.NonEmptyBlocks>0)
            {
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
                        workItem,
                        priority ? Globals.Watch.ElapsedTicks : long.MaxValue)
                    );
            }
            else
            {
                OnGenerateVerticesDone(this);
            }

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