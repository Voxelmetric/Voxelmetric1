using UnityEngine;
using System.Collections.Generic;

public class SaveProgress
{
    List<BlockPos> chunksToSave = new List<BlockPos>();
    int totalChunksToSave = 0;
    int progress = 0;

    public SaveProgress(ICollection<BlockPos> chunks)
    {
        chunksToSave.AddRange(chunks);
        totalChunksToSave = chunks.Count;
    }

    public int GetProgress()
    {
        return progress;
    }

    public void SaveCompleteForChunk(BlockPos pos)
    {
        chunksToSave.Remove(pos.ContainingChunkCoordinates());
        progress = Mathf.FloorToInt(((totalChunksToSave - chunksToSave.Count) / ((float)totalChunksToSave)) * 100);
    }

}
