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

    [HideInInspector]
    public int worldIndex;

    void Start()
    {
        config = new ConfigLoader<WorldConfig>(new string[] { "Worlds" }).GetConfig(worldConfig);

        networking.StartConnections(this);
        chunks = new WorldChunks(this);
        blocks = new WorldBlocks(this);

        worldIndex = Voxelmetric.resources.AddWorld(this);

        textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
        blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);
        terrainGen = new TerrainGen(this, config.layerFolder);

        gameObject.GetComponent<Renderer>().material.mainTexture = textureIndex.atlas;

        chunksLoop = new ChunksLoop(this);
    }

    void Update()
    {
        chunksLoop.MainThreadLoop();
    }

    void OnApplicationQuit()
    {
        chunksLoop.isPlaying = false;

        if (networking.isServer)
        {
            if (networking.allowConnections)
            {
                networking.server.DisconnectClients();
            }
        }
        else
        {
            networking.client.Disconnect();
        }
    }
}
