using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using SimplexNoise;
using System;

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

    public bool delayStart;
    
    //Multi threading must be disabled on web builds
    private bool useMultiThreading;
    //Experimental: Run mesh building and terrain gen using coroutines to keep framerate up
    private bool useCoroutines;

    private Block voidBlock;
    private Block airBlock;

    private EmptyChunk emptyChunk;

    private Action cbConfigure;
    private Action<BlockPos> cbBlockChanged;
    private Action<Chunk> cbChunkContentsGenerated;

    // TODO Should be an enum, UseMultiThreading, UseCoroutines, Neither because both won't work
    public bool UseMultiThreading {
        get { return useMultiThreading; }
        set { useMultiThreading = value; }
    }
    public bool UseCoroutines {
        get { return useCoroutines; }
        set { useCoroutines = value; }
    }

    public Block Void {
        get {
            if (voidBlock == null)
                voidBlock = Block.New(Block.VoidType, this);
            return voidBlock;
        }
    }

    public Block Air {
        get {
            if (airBlock == null)
                airBlock = Block.New(Block.AirType, this);
            return airBlock;
        }
    }

    public EmptyChunk EmptyChunk {
        get {
            if (emptyChunk == null) {
                emptyChunk = new EmptyChunk(this, new BlockPos());
            }
            return emptyChunk;
        }
    }

    public bool IsStarted { get { return chunksLoop != null; } }

    public object Extensions { get; set; }

    public World() {
        chunks = new WorldChunks(this);
        blocks = new WorldBlocks(this);
    }

    void Start()
    {
        if (!delayStart)
            StartWorld();
    }

    void Update()
    {
        if (chunksLoop != null)
            chunksLoop.MainThreadLoop();
    }

    void OnApplicationQuit()
    {
        StopWorld();
    }

    public void Configure() {
        config = new ConfigLoader<WorldConfig>(new string[] { "Worlds" }).GetConfig(worldConfig);
        textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
        blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);
        //blockIndex.DebugLog();
        if(cbConfigure != null)
            cbConfigure();
    }

    /// <summary>
    /// Start the world
    /// </summary>
    /// <remarks>
    /// chunksLoop should always not be null when the world is started
    /// </remarks>
    public void StartWorld() {
        if (IsStarted)
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
        if (!IsStarted)
            return;
        chunksLoop.Stop();
        networking.EndConnections();
        chunksLoop = null;
    }
    public Chunk GetChunk(BlockPos pos) {
        return chunks.Get(pos);
    }

    public Block GetBlock(BlockPos pos) {
        return blocks.Get(pos);
    }

    public Block SetBlock(BlockPos pos, ushort type) {
        var block = Block.New(type, this);
        if (!SetBlock(pos, block))
            return null;
        return block;
    }

    public Block SetBlock(BlockPos pos, string name) {
        var block = Block.New(name, this);
        if(!SetBlock(pos, block))
            return null;
        return block;
    }

    public bool SetBlock(BlockPos pos, Block block) {
        Chunk chunk = chunks.Get(pos);
        if(chunk == null)
            return false;

        chunk.world.blocks.Set(pos, block);
        OnBlockChanged(pos);
        return true;
    }

    public void OnBlockChanged(BlockPos blockpos) {
        if ( cbBlockChanged != null )
            cbBlockChanged(blockpos);
    }

    public BlockPos GetBlockPos(Vector3 pos) {
        //Transform the positiony to match the rotation and position of the world:
        pos -= transform.position;
        pos = Quaternion.Inverse(gameObject.transform.rotation) * pos;
        BlockPos bPos = pos;
        return bPos;
    }

    /// <summary>
    /// Tests if the block at pos can be walked on by an entity of entityHeight
    /// </summary>
    /// <remarks>
    /// Also tests that the blocks above pos can be walked through if entityHeight > 0
    /// </remarks>
    /// <param name="pos"></param>
    /// <param name="entityHeight"></param>
    /// <returns></returns>
    public bool IsWalkable(BlockPos pos, int entityHeight) {
        Block block = blocks.Get(pos);
        if (!block.canBeWalkedOn)
            return false;
        pos.y++;
        return CanBeWalkedThrough(pos, entityHeight);
    }

    /// <summary>
    /// Tests if the block at pos can be walked through by an entity of entityHeight
    /// </summary>
    /// <remarks>
    /// Also test that the blocks above pos can be walked through if entityHeight > 1
    /// </remarks>
    /// <param name="pos"></param>
    /// <param name="entityHeight"></param>
    /// <returns></returns>
    public bool CanBeWalkedThrough(BlockPos pos, int entityHeight) {
        for(int y = 0; y < entityHeight; y++) {
            if(!blocks.Get(pos.Add(0, y, 0)).canBeWalkedThrough)
                return false;
        }
        return true;
    }

    /// <summary>
    /// Tests if the blocks at pos and above are are solid to an entity of entityHeight
    /// </summary>
    /// <remarks>
    /// Always returns false for entityHeight <= 0
    /// </remarks>
    /// <param name="pos"></param>
    /// <param name="entityHeight"></param>
    /// <returns></returns>
    public bool IsSolid(BlockPos pos, int entityHeight) {
        for(int y = 0; y < entityHeight; y++) {
            if(blocks.Get(pos.Add(0, y, 0)).solid)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Find the cost of an entity of entityHeight moving from 'from' to 'to'
    /// </summary>
    /// <remarks>
    /// 'from' is assumed to be adjacent to 'to'
    /// </remarks>
    /// <param name="from"></param>
    /// <param name="to"></param>
    /// <param name="entityHeight"></param>
    /// <returns></returns>
    public float MovementCost(BlockPos from, BlockPos to, int entityHeight) {
        var below = to.Add(0, -1, 0);
        if(!IsWalkable(below, entityHeight))
            return 0f;
        BlockPos dir = to - from;
        if(Math.Abs(dir.x) == 1 && Math.Abs(dir.z) == 1) {
            // Diagonal move -- need to check off diags too
            if(!CanBeWalkedThrough(to.Subtract(dir.x, 0, 0), entityHeight))
                return 0f;
            if(!CanBeWalkedThrough(to.Subtract(0, 0, dir.z), entityHeight))
                return 0f;
        }
        Block block = GetBlock(below);
        return block.movementCost;
    }

    /// <summary>
    /// Find position that canBeWalkedOn
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool FindGroundPos(ref Vector3 pos, int entityHeight) {
        BlockPos blockPos = pos;
        bool found = FindGroundPos(ref blockPos, entityHeight);
        pos = blockPos;
        return found;
    }

    /// <summary>
    /// Find position that canBeWalkedOn
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool FindGroundPos(ref BlockPos pos, int entityHeight) {
        BlockPos blockPosOrg = GetBlockPos(pos);
        
        int offset = 0, maxOffset = 1024;
        bool found = false;
        while (!found) {
            for (int sgn = -1; sgn < 2; sgn += 2) {
                BlockPos blockPos = blockPosOrg;
                blockPos.y += offset*sgn;

                Block block = blocks.Get(blockPos);
                if ( block.Type == Void.Type )
                    break;

                if ( IsWalkable(blockPos, entityHeight) ) {
                    found = true;
                    //blockPos.y += 1;
                    pos = blockPos;
                    break;
                }
            }
            offset++;
            if (offset > maxOffset)
                break;
        }
        /*if ( offset > 10 )
            Debug.Log("offset to ground was " + offset + " for " + posOrg + " giving " + pos);*/
        return found;
    }

    public void RegisterConfigure(Action callback) {
        cbConfigure += callback;
    }

    public void UnregisterConfigure(Action callback) {
        cbConfigure -= callback;
    }

    public void RegisterBlockChanged(Action<BlockPos> callback) {
        cbBlockChanged += callback;
    }

    public void UnregisterBlockChanged(Action<BlockPos> callback) {
        cbBlockChanged -= callback;
    }

    public void RegisterChunkContentsGenerated(Action<Chunk> callback) {
        cbChunkContentsGenerated += callback;
    }

    public void UnregisterChunkContentsGenerated(Action<Chunk> callback) {
        cbChunkContentsGenerated -= callback;
    }

    internal void FireContentsGenerated(Chunk chunk) {
        if(cbChunkContentsGenerated != null)
            cbChunkContentsGenerated(chunk);
    }
}
