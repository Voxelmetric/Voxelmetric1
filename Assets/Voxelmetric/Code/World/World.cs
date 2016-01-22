using UnityEngine;
using System.Threading;
using System.Collections.Generic;
using SimplexNoise;

public class World : MonoBehaviour {

    public string worldConfig;

    public WorldConfig config;
    public BlockIndex blockIndex;
    public TextureIndex textureIndex;

    /// <summary> True if this world is hosted by the player, not someone else </summary>
    public bool isServer = true;
    public bool allowConnections = true;

    //This world name is used for the save file name and as a seed for random noise
    // leave empty to override with 
    public string worldName = "world";

    public Noise noise;
    public WorldChunks chunks;
    public WorldBlocks blocks;
    public ChunksLoop chunksLoop;
    public VmClient client;
    public VmServer server;
    public VmNetworking networking;
    public TerrainGen terrainGen;

    [HideInInspector]
    public int worldIndex;

    void Start()
    {
        config = new ConfigLoader<WorldConfig>(new string[] {"Worlds"}).GetConfig(worldConfig);
        noise = new Noise(worldName);

        chunks = new WorldChunks(this);
        blocks = new WorldBlocks(this);
        networking = new VmNetworking();

        if (!isServer)
        {
            client = new VmClient(this);
        }
        else if(allowConnections)
        {
            server = new VmServer(this);
        }

        worldIndex = Voxelmetric.resources.worlds.Count;
        Voxelmetric.resources.AddWorld(this);

        textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
        blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);

        terrainGen = new TerrainGen(this, config.layerFolder);
        chunksLoop = new ChunksLoop(this);
    }

    void Update()
    {
    }

    void LateUpdate()
    {
        chunksLoop.MainThreadLoop();
        //chunks.ChunksUpdate();
    }

    void OnApplicationQuit()
    {
        chunksLoop.isPlaying = false;

        if (isServer)
        {
            if (allowConnections)
            {
                server.DisconnectClients();
            }
        }
        else
        {
            client.Disconnect();
        }
    }
}
