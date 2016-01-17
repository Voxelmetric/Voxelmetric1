using UnityEngine;

public enum Stage {created, terrain, buildMesh, render, ready, saveAndDelete, delete }

[RequireComponent(typeof(MeshFilter))]
[RequireComponent(typeof(MeshRenderer))]
[RequireComponent(typeof(MeshCollider))]
public class Chunk : MonoBehaviour
{
    public World world;
    public BlockPos pos;

    public ChunkBlocks blocks;
    public ChunkLogic  logic;
    public ChunkRender render;

    public Stage _stage;
    public Stage stage {
        get
        {
            return _stage;
        }
        set
        {
            if (_stage != value)
            {
                world.chunksLoop.ChunkStageChanged(this, oldStage: _stage, newStage: value);
            }
            _stage = value;
        }
    }

    public virtual void Start()
    {
        if (render == null)
            render = new ChunkRender(this);

        if (blocks == null)
            blocks = new ChunkBlocks(this);

        if (logic == null)
            logic = new ChunkLogic(this);
    }

    public virtual void StartLoading()
    {
        stage = Stage.terrain;
    }

    public virtual void RegularUpdate()
    {
        if (stage == Stage.created || stage == Stage.delete)
            return;

        logic.TimedUpdated();

        if (logic.GetFlag(Flag.updateNow))
        {
            render.UpdateChunk();
            logic.SetFlag(Flag.updateNow, false);
            logic.SetFlag(Flag.updateSoon, false);
        }

        if (logic.GetFlag(Flag.meshReady))
        {
            logic.SetFlag(Flag.meshReady, false);
            render.RenderMesh();
            render.ClearMeshData();

            logic.SetFlag(Flag.busy, false);
        }
    }

    /// <summary> Updates the chunk either now or as soon as the chunk is no longer busy </summary>
    public void UpdateNow()
    {
        logic.SetFlag(Flag.updateNow, true);
    }

    /// <summary> Tells the chunk to update on it's next timed update. Use this for updates where the
    /// effects don't need to be immediate because it could reduce the number of updates. </summary>
    public void UpdateSoon()
    {
        logic.SetFlag(Flag.updateSoon, true);
    }

    public void MarkForDeletion()
    {
        logic.SetFlag(Flag.markedForDeletion, true);
        world.chunksLoop.AddToDeletionList(this);
    }

    public void ReturnChunkToPool()
    {
        logic.ResetContent();
        render.ResetContent();
        blocks.ResetContent();

        stage = Stage.created;

        world.chunks.Remove(pos);
        world.chunks.AddToChunkPool(gameObject);
    }
}