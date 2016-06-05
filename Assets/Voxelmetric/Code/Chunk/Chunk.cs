using System.Text;
using Assets.Voxelmetric.Code;
using Assets.Voxelmetric.Code.Common.MemoryPooling;

public enum Stage {created, terrain, buildMesh, priorityBuildMesh, render, ready }

public class Chunk
{
    private static int s_id = 0;

    public World world;
    public BlockPos pos;
    public LocalPools pools;

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

    public int ThreadId { get; private set; }

    protected Chunk()
    {
        // Associate chunk with a certain thread and make use of its memory pool
        // This is necessary in order to have lock-free caches
        ThreadId = Globals.WorkPool.GetThreadIDFromIndex(s_id++);
        pools = Globals.WorkPool.GetPool(ThreadId);

        render = new ChunkRender(this);
        blocks = new ChunkBlocks(this);
        logic = new ChunkLogic(this);
    }

    protected virtual void Init(World world, BlockPos pos)
    {
        this.world = world;
        this.pos = pos;
    }

    public static Chunk Create(World world, BlockPos pos)
    {
        Chunk chunk = new Chunk();
        chunk.Init(world, pos);
        return chunk;
    }

    public override string ToString() {
        StringBuilder sb = new StringBuilder();
        sb.Append(world.name);
        sb.Append(", ");
        sb.Append(pos);
        sb.Append(", stage=");
        sb.Append(_stage);
        sb.Append(", blocks=");
        sb.Append(blocks);
        sb.Append(", logic=");
        sb.Append(logic);
        sb.Append(", render=");
        sb.Append(render);
        return sb.ToString();
    }

    public virtual void StartLoading()
    {
        stage = Stage.terrain;
    }

    public virtual void RegularUpdate()
    {
        if (stage != Stage.ready)
            return;

        logic.TimedUpdated();
    }

    /// <summary> Updates the chunk either now or as soon as the chunk is no longer busy </summary>
    public void UpdateNow()
    {
        stage = Stage.priorityBuildMesh;
    }

    public void UpdateSoon()
    {
        stage = Stage.buildMesh;
    }

    public void MarkForDeletion()
    {
        world.chunksLoop.AddToDeletionList(this);
    }
}