using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Core.StateManager;

namespace Voxelmetric.Code.Core.Serialization
{
    public class SaveProgress: IEventListener<ChunkStateExternal>
    {
        private readonly List<Chunk> chunksToSave = new List<Chunk>();
        public readonly int totalChunksToSave = 0;
        private int progress = 0;
        
        public SaveProgress(List<Chunk> chunks)
        {
            if (chunks==null)
                return;

            if(chunks.Count<=0)
            {
                progress = 100;
                return;
            }

            chunksToSave.AddRange(chunks);
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

        private void SaveCompleteForChunk(Chunk chunk)
        {
            chunksToSave.Remove(chunk);
            progress = Mathf.FloorToInt((totalChunksToSave - chunksToSave.Count) / (float)totalChunksToSave * 100);
        }

        void IEventListener<ChunkStateExternal>.OnNotified(IEventSource<ChunkStateExternal> source, ChunkStateExternal evt)
        {
            // Unsubscribe from any further events
            ChunkStateManagerClient stateManager = (ChunkStateManagerClient)source;
            stateManager.Unregister(this);

            Assert.IsTrue(evt==ChunkStateExternal.Saved);
            if (evt==ChunkStateExternal.Saved)
            {
                if (!chunksToSave.Contains(stateManager.chunk))
                    return;
                SaveCompleteForChunk(stateManager.chunk);
            }
        }
    }
}
