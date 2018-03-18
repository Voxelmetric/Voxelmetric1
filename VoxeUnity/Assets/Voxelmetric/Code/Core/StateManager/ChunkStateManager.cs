using System.Runtime.CompilerServices;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Core.Serialization;

namespace Voxelmetric.Code.Core.StateManager
{
    public abstract class ChunkStateManager: ChunkEventSource
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
        private bool m_removalRequested;

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
            m_removalRequested = false;

            m_taskRunning = false;

            if (m_save!=null)
                m_save.Reset();
        }

        public abstract void Update();

        #region Pending states

        public void SetStatePending(ChunkState state)
        {
            if (m_removalRequested && (state == ChunkState.PrepareSaveData || state == ChunkState.Remove))
                return;
            if (state == ChunkState.Remove)
            {
                m_removalRequested = true;
                if (Features.SerializeChunkWhenUnloading)
                    m_pendingStates = m_pendingStates.Set(ChunkState.PrepareSaveData);
            }            

            m_pendingStates = m_pendingStates.Set(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ResetStatePending(ChunkState state)
        {
           m_pendingStates = m_pendingStates.Reset(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected bool IsStatePending(ChunkState state)
        {
            return m_pendingStates.Check(state);
        }

        #endregion

        #region Completed states

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void SetStateCompleted(ChunkState state)
        {
            m_completedStates = m_completedStates.Set(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        protected void ResetStateCompleted(ChunkState state)
        {
            m_completedStates = m_completedStates.Reset(state);
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public bool IsStateCompleted(ChunkState state)
        {
            return m_completedStates.Check(state);
        }

        #endregion

        public bool CanUpdate
        {
            get
            {
                // Do not do any processing as long as there is any task still running
                // Note that this check is not thread-safe because this value can be changed from a different thread. However,
                // we do not care. The worst thing that can happen is that we read a value which is one frame old...
                // Thanks to this relaxed approach we do not need any synchronization primitives anywhere.
                if (m_taskRunning)
                    return false;

                // Once this Chunk is marked as finished we ignore any further requests and won't perform any updates
                return !m_completedStates.Check(ChunkState.Remove);
            }
        }

        public bool IsSavePossible
        {
            get {
                // Serialization must be enabled
                if (!Features.UseSerialization)
                    return false;

                // Chunk has to be generated first before we can save it
                if (!m_completedStates.Check(ChunkState.Generate))
                    return false;

                // When doing a pure differential serialization chunk needs to be modified before we can save it
                return
                    !Features.UseDifferentialSerialization ||
                    Features.UseDifferentialSerialization_ForceSaveHeaders ||
                    chunk.Blocks.modifiedBlocks.Count>0;
            }
        }

        public bool IsUpdateBlocksPossible
        {
            get {
                // Chunk has to be generated first before we can update its blocks
                if (!m_completedStates.Check(ChunkState.Generate))
                    return false;

                // Never update during saving
                return !m_pendingStates.Check(ChunkState.PrepareSaveData) && !m_pendingStates.Check(ChunkState.SaveData);
            }
        }

        public abstract void SetMeshBuilt();
        public abstract void SetColliderBuilt();
    }
}
