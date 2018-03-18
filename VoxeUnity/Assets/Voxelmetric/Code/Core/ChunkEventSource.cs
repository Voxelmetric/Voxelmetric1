using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;

namespace Voxelmetric.Code.Core
{
    public class ChunkEventSource: IEventSource<ChunkStateExternal>
    {
        //! List of external listeners
        private List<IEventListener<ChunkStateExternal>> m_listenersExternal;
     
        protected ChunkEventSource()
        {
            Clear();
        }

        public void Clear()
        {
            
            m_listenersExternal = new List<IEventListener<ChunkStateExternal>>();
        }

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
    }
}
