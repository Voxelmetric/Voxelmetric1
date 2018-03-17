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
        //! States waiting to be processed
        private ChunkState m_pendingStates;
        //! Tasks already executed
        private ChunkState m_completedStates;
        
        //! If true, removal of chunk has been requested and no further requests are going to be accepted
        protected bool m_isSaveNeeded;

        protected ChunkStateManager(Chunk chunk)
        {
            this.chunk = chunk;
            if(Features.UseSerialization)
                m_save = new Save(chunk);
        }

        public virtual void Init()
        {
            // Request this chunk to be generated
            m_pendingStates = m_pendingStates.Set(ChunkState.LoadData);
        }

        public virtual void Reset()
        {
            Clear();

            m_pendingStates = m_pendingStates.Reset();
            m_completedStates = m_completedStates.Reset();
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

            // Once this Chunk is marked as finished we ignore any further requests and won't perform any updates
            return !m_completedStates.Check(ChunkState.Remove);
        }

        public abstract void Update();

        #region Pending states

        public void SetStatePending(ChunkState state)
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
                    m_pendingStates = m_pendingStates.Set(ChunkState.Remove);
                    if (Features.SerializeChunkWhenUnloading)
                        m_pendingStates = m_pendingStates.Set(ChunkState.PrepareSaveData);
                }
                break;
            }

            m_pendingStates = m_pendingStates.Set(state);
        }

        protected void ResetStatePending(ChunkState state)
        {
           m_pendingStates = m_pendingStates.Reset(state);
        }

        protected bool IsStatePending(ChunkState state)
        {
            return m_pendingStates.Check(state);
        }

        #endregion

        #region Completed states

        protected void SetStateCompleted(ChunkState state)
        {
            m_completedStates = m_completedStates.Set(state);
        }

        protected void ResetStateCompleted(ChunkState state)
        {
            m_completedStates = m_completedStates.Reset(state);
        }

        public bool IsStateCompleted(ChunkState state)
        {
            return m_completedStates.Check(state);
        }

        #endregion

        public bool IsSavePossible
        {
            get { return m_save!=null && !m_pendingStates.Check(ChunkState.Remove) && m_completedStates.Check(ChunkState.Generate); }
        }

        public bool IsUpdateBlocksPossible
        {
            get { return !m_pendingStates.Check(ChunkState.PrepareSaveData) && !m_pendingStates.Check(ChunkState.SaveData); }
        }

        public abstract void SetMeshBuilt();
        public abstract void SetColliderBuilt();
    }
}
