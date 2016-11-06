using UnityEngine;
using System.Collections.Generic;
using System.Collections;

public class SaveProgress
{
    private List<BlockPos> chunksToSave = new List<BlockPos>();
    public readonly int totalChunksToSave = 0;
    private int progress = 0;

    private List<BlockPos> errorChunks = new List<BlockPos>();
    private List<Chunk> saveChunks;

    public IEnumerable<BlockPos> ErrorChunks { get { return errorChunks; } }

    public SaveProgress(ICollection<BlockPos> chunks, List<Chunk> saveChunks = null)
    {
        this.saveChunks = saveChunks;
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

    public IEnumerator SaveCoroutine() {
        foreach(var chunk in saveChunks) {
            var saver = new Serialization.Saver();
            var e = saver.Save(chunk); while(e.MoveNext()) yield return e.Current;

            if(!saver.IsSaved)
                SaveErrorForChunk(chunk.pos);
            else
                SaveCompleteForChunk(chunk.pos);
            yield return null;
        }
    }

}
