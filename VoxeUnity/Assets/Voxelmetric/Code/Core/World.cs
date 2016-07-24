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
        public TerrainGen terrainGen;

        public Material renderMaterial;
        public PhysicMaterial physicsMaterial;

        void Awake()
        {
            chunks = new WorldChunks(this);
            blocks = new WorldBlocks(this);
        }

        void Start()
        {
            StartWorld();
        }

        void Update()
        {
        }

        void OnApplicationQuit()
        {
            StopWorld();
        }

        public void Configure()
        {
            config = new ConfigLoader<WorldConfig>(new[] { "Worlds" }).GetConfig(worldConfig);

            textureProvider = Voxelmetric.resources.GetTextureProvider(this);
            blockProvider = Voxelmetric.resources.GetBlockProvider(this);

            renderMaterial.mainTexture = textureProvider.atlas;
        }

        private void StartWorld()
        {
            Configure();

            networking.StartConnections(this);
            terrainGen = TerrainGen.Create(this, config.layerFolder);
        }

        private void StopWorld()
        {
            networking.EndConnections();
        }
    }
}
