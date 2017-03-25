using UnityEngine;
using Voxelmetric.Code.Data_types;
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
            VerifyConfig();

            textureProvider = Voxelmetric.resources.GetTextureProvider(this);
            blockProvider = Voxelmetric.resources.GetBlockProvider(this);

            renderMaterial.mainTexture = textureProvider.atlas;
        }

        private void VerifyConfig()
        {
            // minY can't be greater then maxY
            if (config.minY>config.maxY)
            {
                int tmp = config.minY;
                config.maxY = config.minY;
                config.minY = tmp;
            }

            if (config.minY!=config.maxY)
            {
                // Make sure there is at least one chunk worth of space in the world on the Y axis
                if (config.maxY-config.minY<Env.ChunkSize)
                    config.maxY = config.minY + Env.ChunkSize;
            }
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

        public bool IsWorldCoordsRestricted()
        {
            return config.minY!=config.maxY;
        }

        public void CapCoordYInsideWorld(ref int minY, ref int maxY)
        {
            if (config.minY!=config.maxY)
            {
                int offset = Env.ChunkSize; // We always load one more chunk
                minY = Mathf.Max(minY, config.minY-offset);
                maxY = Mathf.Min(maxY, config.maxY+offset);
            }
        }

        public bool IsCoordInsideWorld(Vector3Int pos)
        {
            int offset = Env.ChunkSize; // We always load one more chunk
            return config.minY==config.maxY || (pos.y>=config.minY-offset && pos.y<=config.maxY+offset);
        }
    }
}
