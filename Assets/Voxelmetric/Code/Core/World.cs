using UnityEngine;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.VM;

namespace Voxelmetric.Code.Core
{
    public class World : MonoBehaviour
    {
        public string worldConfig = "default";
        public WorldConfig config;

        //This world name is used for the save file name and as a seed for random noise
        public string worldName = "world";

        public WorldChunks chunks;
        public WorldBlocks blocks;
        public VmNetworking networking = new VmNetworking();

        public BlockProvider blockProvider;
        public TextureProvider textureProvider;
        public ChunksLoop chunksLoop;
        public TerrainGen terrainGen;

        public Material chunkMaterial;
        
        public bool UseFrustumCulling;

        void Awake()
        {
            chunks = new WorldChunks(this);
            blocks = new WorldBlocks(this);
        }

        void Start()
        {
            Profiler.maxNumberOfSamplesPerFrame = Mathf.Max(Profiler.maxNumberOfSamplesPerFrame, 1000000);
            StartWorld();
        }

        void Update()
        {
            if (chunksLoop != null)
                chunksLoop.Update();
        }

        void OnApplicationQuit()
        {
            StopWorld();
        }

        public void Configure()
        {
            config = new ConfigLoader<WorldConfig>(new[] { "Worlds" }).GetConfig(worldConfig);

            textureProvider = Voxelmetric.resources.GetTextureProvider(this);
            textureProvider.Init(config);

            blockProvider = Voxelmetric.resources.GetBlockProvider(this);
            blockProvider.Init(config.blockFolder, this);

            chunkMaterial.mainTexture = textureProvider.atlas;
        }

        private void StartWorld()
        {
            if (chunksLoop != null)
                return;

            Configure();

            networking.StartConnections(this);
            terrainGen = new TerrainGen(this, config.layerFolder);
            chunksLoop = new ChunksLoop(this);
        }

        private void StopWorld()
        {
            if (chunksLoop == null)
                return;

            chunksLoop.Stop();
            networking.EndConnections();
            chunksLoop = null;
        }
    }
}
