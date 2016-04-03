using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using SimplexNoise;

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

    private bool useMultiThreading;

    private Coroutine terrainLoopCoroutine;
    private Coroutine buildMeshLoopCoroutine;

    private Block voidBlock;
    private Block airBlock;

    public bool UseMultiThreading { get { return useMultiThreading; } }

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

    public World() {
        chunks = new WorldChunks(this);
        blocks = new WorldBlocks(this);
    }

    void Start()
    {
        if (!delayStart)
            StartWorld(Config.Toggle.UseMultiThreadingDefault);
    }

    void Update()
    {
        if (chunksLoop != null) {
            if (!useMultiThreading) {
                if (terrainLoopCoroutine == null)
                    terrainLoopCoroutine = StartCoroutine(chunksLoop.TerrainLoopCoroutine());
                if (buildMeshLoopCoroutine == null)
                    buildMeshLoopCoroutine = StartCoroutine(chunksLoop.BuildMeshLoopCoroutine());
            }

            chunksLoop.MainThreadLoop();
        }
    }

    void OnApplicationQuit()
    {
        StopWorld();
    }

    public void Configure() {
        config = new ConfigLoader<WorldConfig>(new string[] { "Worlds" }).GetConfig(worldConfig);
        textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
        blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);
    }

    public void StartWorld(bool useMultiThreading) {
        if (chunksLoop != null)
            return;
        Configure();
        this.useMultiThreading = useMultiThreading;

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

    public BlockPos getBlockPos(Vector3 pos) {
        //Transform the positiony to match the rotation and position of the world:
        pos -= transform.position;
        pos = Quaternion.Inverse(gameObject.transform.rotation) * pos;
        BlockPos bPos = pos;
        return bPos;
    }

    public bool IsWalkable(BlockPos pos, int entityHeight) {
        Block block = blocks.Get(pos);
        if (!block.canBeWalkedOn)
            return false;

        for (int y = 1; y < entityHeight + 1; y++) {
            block = blocks.Get(pos.Add(0, y, 0));

            if (!block.canBeWalkedThrough) {
                return false;
            }
        }

        return true;

    }

    /// <summary>
    /// Find position that canBeWalkedOn
    /// </summary>
    /// <param name="pos"></param>
    /// <returns></returns>
    public bool findGroundPos(ref Vector3 pos, int entityHeight) {
        BlockPos blockPosOrg = getBlockPos(pos);
        
        int offset = 0, maxOffset = 1024;
        bool found = false;
        while (!found) {
            for (int sgn = -1; sgn < 2; sgn += 2) {
                BlockPos blockPos = blockPosOrg;
                blockPos.y += offset*sgn;

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
}
