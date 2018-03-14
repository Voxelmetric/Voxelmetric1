using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core
{
    public class ChunkEvent :
        IEventSource<ChunkState>,
        IEventSource<ChunkStateExternal>,
        IEventListener<ChunkState>
    {
        //! Number of registered listeners
        protected int ListenerCount { get; private set; }
        protected int ListenerCountMax { get; set; }

        //! List of external listeners
        private List<IEventListener<ChunkStateExternal>> m_listenersExternal;
        private readonly ChunkEvent[] m_listeners;
        //! List of chunk listeners
        public ChunkEvent[] Listeners
        {
            get { return m_listeners; }
        }

        protected ChunkEvent()
        {
            m_listeners = Helpers.CreateArray1D<ChunkEvent>(6);
            m_listenersExternal = new List<IEventListener<ChunkStateExternal>>();

            Clear();
        }

        public void Clear()
        {
            ListenerCount = 0;
            ListenerCountMax = 0;

            for (int i = 0; i < Listeners.Length; i++)
                Listeners[i] = null;
            m_listenersExternal = new List<IEventListener<ChunkStateExternal>>();
        }

        #region IEventSource<ChunkState>

        public bool Register(IEventListener<ChunkState> listener)
        {
            if (listener==null || listener==this)
                return false;

            // Determine neighbors's direction as compared to current chunk
            Chunk chunk = ((ChunkStateManager)this).chunk;
            Chunk chunkNeighbor = ((ChunkStateManager)listener).chunk;
            Vector3Int p = chunk.pos-chunkNeighbor.pos;
            Direction dir = Direction.up;
            if (p.x<0)
                dir = Direction.east;
            else if (p.x>0)
                dir = Direction.west;
            else if (p.z<0)
                dir = Direction.north;
            else if (p.z>0)
                dir = Direction.south;
            else if (p.y>0)
                dir = Direction.down;

            ChunkEvent chunkListener = (ChunkEvent)listener;
            ChunkEvent l = Listeners[(int)dir];

            // Do not register if already registred
            if (l==listener)
                return false;

            // Subscribe in the first free slot
            if (l==null)
            {
                ++ListenerCount;
                Assert.IsTrue(ListenerCount<=6);
                Listeners[(int)dir] = chunkListener;
                return true;
            }

            // We want to register but there is no free space
            Assert.IsTrue(false);

            return false;
        }

        public bool Unregister(IEventListener<ChunkState> listener)
        {
            if (listener == null || listener == this)
                return false;

            // Determine neighbors's direction as compared to current chunk
            Chunk chunk = ((ChunkStateManager)this).chunk;
            Chunk chunkNeighbor = ((ChunkStateManager)listener).chunk;
            Vector3Int p = chunk.pos - chunkNeighbor.pos;
            Direction dir = Direction.up;
            if (p.x < 0)
                dir = Direction.east;
            else if (p.x > 0)
                dir = Direction.west;
            else if (p.z < 0)
                dir = Direction.north;
            else if (p.z > 0)
                dir = Direction.south;
            else if (p.y > 0)
                dir = Direction.down;

            ChunkEvent chunkListener = (ChunkEvent)listener;
            ChunkEvent l = Listeners[(int)dir];

            // Do not unregister if it's something else than we expected
            if (l != listener && l != null)
            {
                Assert.IsTrue(false);
                return false;
            }

            // Only unregister already registered sections
            if (l == listener)
            {
                --ListenerCount;
                Assert.IsTrue(ListenerCount >= 0);
                Listeners[(int)dir] = null;
                return true;
            }

            return false;
        }
        
        public void NotifyAll(ChunkState state)
        {
            // Notify each registered listener
            for (int i = 0; i < Listeners.Length; i++)
            {
                ChunkEvent l = Listeners[i];
                if (l != null)
                    l.OnNotified(this, state);
            }
        }

        #endregion

        #region IEventSource<ChunkStateExternal>

        public bool Register(IEventListener<ChunkStateExternal> listener)
        {
            Assert.IsTrue(listener != null);
            if (!m_listenersExternal.Contains(listener))
            {
                m_listenersExternal.Add(listener);
                return true;
            }

            return false;
        }

        public bool Unregister(IEventListener<ChunkStateExternal> listener)
        {
            Assert.IsTrue(listener != null);
            return m_listenersExternal.Remove(listener);
        }

        public void NotifyAll(ChunkStateExternal evt)
        {
            for (int i = 0; i < m_listenersExternal.Count; i++)
            {
                IEventListener<ChunkStateExternal> listener = m_listenersExternal[i];
                listener.OnNotified(this, evt);
            }
        }

        #endregion

        #region IEventListener<ChunkState>

        public virtual void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            throw new NotImplementedException();
        }

        #endregion
    }
}
