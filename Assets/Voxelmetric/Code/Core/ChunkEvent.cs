using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.Events;

namespace Voxelmetric.Code.Core
{
    public class ChunkEvent :
        IEventSource<ChunkState>, IEventListener<ChunkState>,
        IEventSource<ChunkStateExternal>
    {
        //! List of chunk listeners
        private readonly ChunkEvent[] m_listeners;
        //! Number of registered listeners
        protected int Listeners { get; private set; }
        //! List of external listeners
        private readonly Dictionary<ChunkStateExternal, List<IEventListener<ChunkStateExternal>>> m_listenersExternal;

        protected ChunkEvent()
        {
            m_listeners = Helpers.CreateArray1D<ChunkEvent>(6);
            m_listenersExternal = new Dictionary<ChunkStateExternal, List<IEventListener<ChunkStateExternal>>>
            {
                {ChunkStateExternal.Saved, new List<IEventListener<ChunkStateExternal>>()}
            };

            Clear();
        }

        public void Clear()
        {
            Listeners = 0;
            for (int i = 0; i < m_listeners.Length; i++)
                m_listeners[i] = null;

            foreach (var pair in m_listenersExternal)
                pair.Value.Clear();
        }

        public bool Subscribe(IEventListener<ChunkState> listener, ChunkState evt, bool registerListener)
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
                }

                // Subscribe in the first free slot
                for (int i = 0; i < m_listeners.Length; i++)
                {
                    ChunkEvent l = m_listeners[i];
                    if (l==null)
                    {
                        ++Listeners;
                        Assert.IsTrue(Listeners<=6);
                        m_listeners[i] = chunkListener;
                        return true;
                    }
                }

                // We want to register but there is no free space
                Assert.IsTrue(false);
            }
            // Unregister
            else
            {
                // Only unregister already registered sections
                for (int i = 0; i < m_listeners.Length; i++)
                {
                    ChunkEvent l = m_listeners[i];

                    if (l==listener)
                    {
                        --Listeners;
                        Assert.IsTrue(Listeners >= 0);
                        m_listeners[i] = null;
                        return true;
                    }
                }
            }

            return false;
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

        public bool Subscribe(IEventListener<ChunkStateExternal> listener, ChunkStateExternal evt, bool register)
        {
            // Retrieve a list of listeners for a given event
            List<IEventListener<ChunkStateExternal>> evtListeners;
            m_listenersExternal.TryGetValue(evt, out evtListeners);

            // Add/remove listener if possible
            bool listenerRegistered = evtListeners.Contains(listener);
            if (register && !listenerRegistered)
            {
                evtListeners.Add(listener);
                return true;
            }
            if (!register && listenerRegistered)
            {
                evtListeners.Remove(listener);
                return true;
            }

            return false;
        }

        public void NotifyAll(ChunkStateExternal evt)
        {
            // Retrieve a list of listeners for a given event
            List<IEventListener<ChunkStateExternal>> evtListeners;
            m_listenersExternal.TryGetValue(evt, out evtListeners);

            // Nofity each listener
            for (int i = 0; i < evtListeners.Count; i++)
            {
                IEventListener<ChunkStateExternal> listener = evtListeners[i];
                listener.OnNotified(this, evt);
            }
        }

        public void NotifyOne(IEventListener<ChunkStateExternal> listener, ChunkStateExternal evt)
        {
            // Retrieve a list of listeners for a given event
            List<IEventListener<ChunkStateExternal>> evtListeners;
            m_listenersExternal.TryGetValue(evt, out evtListeners);

            // Notify our listener
            for (int i = 0; i < evtListeners.Count; i++)
            {
                IEventListener<ChunkStateExternal> l = evtListeners[i];
                if (l == listener)
                {
                    listener.OnNotified(this, evt);
                    return;
                }
            }
        }
    }
}
