using System;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Events;

namespace Voxelmetric.Code.Core
{
    public class ChunkEvent : IEventSource<ChunkState>, IEventListener<ChunkState>
    {
        //! List of listeners
        private readonly ChunkEvent[] m_listeners;
        //! Number of registered listeners
        protected int Listeners { get; private set; }

        protected ChunkEvent()
        {
            m_listeners = Helpers.CreateArray1D<ChunkEvent>(6);

            Clear();
        }

        public void Clear()
        {
            Listeners = 0;
            for (int i = 0; i < m_listeners.Length; i++)
                m_listeners[i] = null;
        }

        public bool Subscribe(IEventListener<ChunkState> listener, bool registerListener)
        {
            if (listener==null || listener==this)
                return false;

            ChunkEvent chunkListener = (ChunkEvent)listener;

            // Register
            if (registerListener)
            {
                // Make sure this section is not registered yet
                for (int i = 0; i<m_listeners.Length; i++)
                {
                    ChunkEvent l = m_listeners[i];

                    // Do not register if already registred
                    if (l==listener)
                        return false;

                    if (l==null)
                    {
                        m_listeners[i] = chunkListener;
                        return true;
                    }
                }

                // We want to register but there is no free space
                Assert.IsTrue(false);
                return false;
            }
            // Unregister
            else
            {
                // Only unregister already registered sections
                for (int i = 0; i < m_listeners.Length; i++)
                {
                    ChunkEvent l = m_listeners[i];

                    // Do not register if already registred
                    if (l == listener)
                        return true;
                }

                return false;
            }
        }
        
        public void NotifyAll(ChunkState state)
        {
            // Notify each registered listener
            for (int i = 0; i < m_listeners.Length; i++)
            {
                ChunkEvent l = m_listeners[i];
                if (l != null)
                   l.OnNotified(this, state);
            }
        }

        public void NotifyOne(IEventListener<ChunkState> listener, ChunkState state)
        {
            // Notify one of the listeners
            for (int i = 0; i < m_listeners.Length; i++)
            {
                ChunkEvent l = m_listeners[i];
                if (l==listener)
                {
                    l.OnNotified(this, state);
                    return;
                }
            }
        }
        
        public virtual void OnNotified(IEventSource<ChunkState> source, ChunkState state)
        {
            throw new NotImplementedException();
        }
    }
}
