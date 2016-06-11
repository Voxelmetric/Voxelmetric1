using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common.Events;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Serialization
{
    public class SaveProgress: IEventListener<ChunkStateExternal>
    {
        private List<Chunk> chunksToSave = new List<Chunk>();
        public readonly int totalChunksToSave = 0;
        private int progress = 0;
        
        public SaveProgress(ICollection<Chunk> chunks)
        {
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
                chunk.Subscribe(this, ChunkStateExternal.Saved, true);
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
            Chunk chunk = source as Chunk;
            chunk.Subscribe(this, evt, false);

            Assert.IsTrue(evt==ChunkStateExternal.Saved);
            if (evt==ChunkStateExternal.Saved)
            {
                if (!chunksToSave.Contains(chunk))
                    return;
                SaveCompleteForChunk(chunk);
            }
        }
    }
}
