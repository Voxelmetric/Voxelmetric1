using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Core.Serialization;

namespace Voxelmetric.Code.Core.StateManager
{
    public abstract class ChunkStateManager: ChunkEvent
    {
        public Chunk chunk { get; private set; }
        
        //! Save handler for chunk
        protected readonly Save m_save;

        //! Specifies whether there's a task running on this Chunk
        protected volatile bool m_taskRunning;
        //! Next state after currently finished state
        protected ChunkState m_nextState;
        //! States waiting to be processed
        protected ChunkState m_pendingStates;
        //! Tasks already executed
        protected ChunkState m_completedStates;
        //! Just like m_completedStates, but it is synchronized on the main thread once a check for m_taskRunning is passed
        protected ChunkState m_completedStatesSafe;
        
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        protected bool m_removalRequested;
        protected bool m_isSaveNeeded;

        protected ChunkStateManager(Chunk chunk)
        {
            this.chunk = chunk;
            if(Utilities.Core.UseSerialization)
                m_save = new Save(chunk);
        }

        public virtual void Init()
        {
            // Request this chunk to be generated
            OnNotified(this, ChunkState.LoadData);
        }

        public virtual void Reset()
        {
            Clear();

            m_nextState = m_nextState.Reset();
            m_pendingStates = m_pendingStates.Reset();
            m_completedStates = m_completedStates.Reset();
            m_completedStatesSafe = m_completedStates;
            m_removalRequested = false;
            m_isSaveNeeded = false;

            m_taskRunning = false;

            if (m_save!=null)
                m_save.Reset();
        }

        public bool CanUpdate()
        {
            // Do not do any processing as long as there is any task still running
            // Note that this check is not thread-safe because this value can be changed from a different thread. However,
            // we do not care. The worst thing that can happen is that we read a value which is one frame old. So be it.
            // Thanks to being this relaxed approach we do not need any synchronization primitives at all.
            if (m_taskRunning)
                return false;

            // Synchronize the value with what we have on a different thread. It would be no big deal not having this at
            // all. However, it is technically more correct.
            m_completedStatesSafe = m_completedStates;

            // Once this Chunk is marked as finished we ignore any further requests and won't perform any updates
            return !m_completedStatesSafe.Check(ChunkState.Remove);
        }

        public abstract void Update();

        public void RequestState(ChunkState state)
        {
            switch (state)
            {
                case ChunkState.PrepareSaveData:
                {
                    m_isSaveNeeded = true;
                }
                break;

                case ChunkState.Remove:
                {
                    if (m_removalRequested)
                        return;
                    m_removalRequested = true;

                    if (Utilities.Core.SerializeChunkWhenUnloading)
                        OnNotified(this, ChunkState.PrepareSaveData);
                    OnNotified(this, ChunkState.Remove);
                }
                break;
            }

            m_pendingStates = m_pendingStates.Set(state);
        }

        public void ResetRequest(ChunkState state)
        {
            m_pendingStates = m_pendingStates.Reset(state);
        }

        public bool IsStateCompleted(ChunkState state)
        {
            return m_completedStatesSafe.Check(state);
        }
        

        public bool IsSavePossible
        {
            get { return m_save!=null && !m_removalRequested && m_completedStatesSafe.Check(ChunkState.Generate); }
        }

        public abstract void SetMeshBuilt();
        public abstract void SetColliderBuilt();
    }
}
