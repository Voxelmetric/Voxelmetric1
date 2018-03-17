using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Core.StateManager;

namespace Voxelmetric.Code.Core.Serialization
{
    public class SaveProgress: IEventListener<ChunkStateExternal>
    {
        private readonly List<Chunk> chunksToSave;
        public readonly int totalChunksToSave = 0;
        private int progress = 0;
        
        public SaveProgress(List<Chunk> chunks)
        {
            if (chunks==null)
                return;

            progress = 0;
            if(chunks.Count<=0)
            {
                progress = 100;
                return;
            }

            chunksToSave = chunks;
            totalChunksToSave = chunksToSave.Count;

            // Register at each chunk
            for (int i = 0; i<chunksToSave.Count; i++)
            {
                Chunk chunk = chunksToSave[i];
                ChunkStateManagerClient stateManager = chunk.stateManager;
                stateManager.Register(this);
            }
        }

        public int GetProgress()
        {
            return progress;
        }

        #region IEventListener

        void IEventListener<ChunkStateExternal>.OnNotified(IEventSource<ChunkStateExternal> source, ChunkStateExternal evt)
        {
            if (evt==ChunkStateExternal.Saved)
            {                
                ChunkStateManagerClient stateManager = (ChunkStateManagerClient)source;                
                chunksToSave.Remove(stateManager.chunk);

                // Unsubscribe from any further events
                stateManager.Unregister(this);

                progress = Mathf.FloorToInt((totalChunksToSave - chunksToSave.Count) / (float)totalChunksToSave * 100);
            }
        }

        #endregion
    }
}
