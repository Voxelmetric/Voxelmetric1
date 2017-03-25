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
        IEventSource<ChunkState>, IEventListener<ChunkState>,
        IEventSource<ChunkStateExternal>
    {
        //! List of chunk listeners
        public ChunkEvent[] Listeners { get; private set; }
        //! Number of registered listeners
        protected int ListenerCount { get; private set; }

        //! List of external listeners
        private readonly Dictionary<ChunkStateExternal, List<IEventListener<ChunkStateExternal>>> m_listenersExternal;

        protected ChunkEvent()
        {
            Listeners = Helpers.CreateArray1D<ChunkEvent>(6);
            m_listenersExternal = new Dictionary<ChunkStateExternal, List<IEventListener<ChunkStateExternal>>>
            {
                {ChunkStateExternal.Saved, new List<IEventListener<ChunkStateExternal>>()}
            };

            Clear();
        }

        public void Clear()
        {
            ListenerCount = 0;
            for (int i = 0; i < Listeners.Length; i++)
                Listeners[i] = null;

            foreach (var pair in m_listenersExternal)
                pair.Value.Clear();
        }

        public bool Subscribe(IEventListener<ChunkState> listener, ChunkState evt, bool registerListener)
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

            // Register
            if (registerListener)
            {
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
            }
            // Unregister
            else
            {
                ChunkEvent l = Listeners[(int)dir];

                // Do not unregister if it's something else than we expected
                if (l!=listener && l!=null)
                {
                    Assert.IsTrue(false);
                    return false;
                }

                // Only unregister already registered sections
                if (l==listener)
                {
                    --ListenerCount;
                    Assert.IsTrue(ListenerCount>=0);
                    Listeners[(int)dir] = null;
                    return true;
                }
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

        public void NotifyOne(IEventListener<ChunkState> listener, ChunkState state)
        {
            // Notify one of the listeners
            for (int i = 0; i < Listeners.Length; i++)
            {
                ChunkEvent l = Listeners[i];
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
            Assert.IsTrue(evtListeners!=null);
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
            Assert.IsTrue(evtListeners!=null);
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
            Assert.IsTrue(evtListeners!=null);
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
