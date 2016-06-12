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

        public BlockIndex blockIndex;
        public TextureIndex textureIndex;
        public ChunksLoop chunksLoop;
        public TerrainGen terrainGen;

        public Material chunkMaterial;
    
        private Block voidBlock;
        private Block airBlock;
        
        public bool UseFrustumCulling;

        public Block Void
        {
            get
            {
                if (voidBlock == null)
                    voidBlock = Block.Create(Block.VoidType, this);
                return voidBlock;
            }
        }

        public Block Air
        {
            get
            {
                if (airBlock == null)
                    airBlock = Block.Create(Block.AirType, this);
                return airBlock;
            }
        }

        void Awake()
        {
            chunks = new WorldChunks(this);
            blocks = new WorldBlocks(this);
        }

        void Start()
        {
            Profiler.maxNumberOfSamplesPerFrame = 1000000;
            StartWorld();
        }

        void Update()
        {
            if (chunksLoop != null)
            {
                chunksLoop.Update();
            }
        }

        void OnApplicationQuit()
        {
            StopWorld();
        }

        public void Configure()
        {
            config = new ConfigLoader<WorldConfig>(new[] { "Worlds" }).GetConfig(worldConfig);
            textureIndex = Voxelmetric.resources.GetOrLoadTextureIndex(this);
            blockIndex = Voxelmetric.resources.GetOrLoadBlockIndex(this);

            chunkMaterial.mainTexture = textureIndex.atlas;
        }

        public void StartWorld()
        {
            if (chunksLoop != null)
                return;
            Configure();

            networking.StartConnections(this);

            terrainGen = new TerrainGen(this, config.layerFolder);
            chunksLoop = new ChunksLoop(this);
        }

        public void StopWorld()
        {
            if (chunksLoop == null)
                return;

            chunksLoop.Stop();
            networking.EndConnections();
            chunksLoop = null;
        }
    }
}
