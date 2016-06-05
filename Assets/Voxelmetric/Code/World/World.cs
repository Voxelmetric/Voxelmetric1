using UnityEngine;

public class World : MonoBehaviour {

    public string worldConfig = "default";
    public WorldConfig config;

    //This world name is used for the save file name and as a seed for random noise
    public string worldName = "world";

    public WorldChunks chunks;
    public WorldBlocks blocks;
    public VmNetworking networking = new VmNetworking();

    public BlockIndex blockIndex;
    public TextureIndex textureIndex;
    public ChunksLoop chunksLoop;
    public TerrainGen terrainGen;
    
    //Multi threading must be disabled on web builds
    public bool useMultiThreading;
    
    private Block voidBlock;
    private Block airBlock;

    public bool UseMultiThreading { get { return useMultiThreading; } }

    public Block Void {
        get {
            if (voidBlock == null)
                voidBlock = Block.Create(Block.VoidType, this);
            return voidBlock;
        }
    }

    public Block Air {
        get {
            if (airBlock == null)
                airBlock = Block.Create(Block.AirType, this);
            return airBlock;
        }
    }

    public World() {
        chunks = new WorldChunks(this);
        blocks = new WorldBlocks(this);
    }

    void Start()
    {
        StartWorld();
    }

    void Update()
    {
        if (chunksLoop != null)
        {
            chunksLoop.MainThreadLoop();
        }
    }

    void OnApplicationQuit()
    {
        StopWorld();
    }

    public void Configure() {
        config = new ConfigLoader<WorldConfig>(new[] { "Worlds" }).GetConfig(worldConfig);
        textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
        blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);
    }

    public void StartWorld()
    {
        if (chunksLoop != null)
            return;
        Configure();

        networking.StartConnections(this);

        terrainGen = new TerrainGen(this, config.layerFolder);

        var renderer = gameObject.GetComponent<Renderer>();
        if (renderer != null)
            renderer.material.mainTexture = textureIndex.atlas;

        chunksLoop = new ChunksLoop(this);
    }

    public void StopWorld() {
        if (chunksLoop == null)
            return;
        chunksLoop.Stop();
        networking.EndConnections();
        chunksLoop = null;
    }

}
