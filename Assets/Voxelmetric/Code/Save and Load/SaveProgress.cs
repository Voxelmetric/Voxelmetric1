using UnityEngine;
using System.Collections.Generic;

public class SaveProgress
{
    private List<BlockPos> chunksToSave = new List<BlockPos>();
    public readonly int totalChunksToSave = 0;
    private int progress = 0;

    private List<BlockPos> errorChunks = new List<BlockPos>();

    public IEnumerable<BlockPos> ErrorChunks { get { return errorChunks; } }

    public SaveProgress(ICollection<BlockPos> chunks)
    {
        chunksToSave.AddRange(chunks);
        totalChunksToSave = chunks.Count;
        if (chunksToSave.Count == 0)
            progress = 100;
    }

    public int GetProgress()
    {
        return progress;
    }

    public void SaveErrorForChunk(BlockPos pos)
    {
        errorChunks.Add(pos);
        SaveCompleteForChunk(pos);
    }

    public void SaveCompleteForChunk(BlockPos pos)
    {
        chunksToSave.Remove(pos.ContainingChunkCoordinates());
        progress = Mathf.FloorToInt(((totalChunksToSave - chunksToSave.Count) / ((float)totalChunksToSave)) * 100);
    }

}
