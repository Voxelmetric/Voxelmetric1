using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Configurable.Structures;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Load_Resources.Textures;
using Voxelmetric.Code.VM;
using Vector3Int = Voxelmetric.Code.Data_types.Vector3Int;

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

        public Material [] renderMaterials;
        public PhysicMaterial [] physicsMaterials;

        public AABBInt Bounds { get; set; }

        private readonly List<ModifyBlockContext> modifyRangeQueue = new List<ModifyBlockContext>();

        private readonly object pendingStructureMutex = new object();
        private readonly Dictionary<Vector3Int, List<StructureContext>> pendingStructures = new Dictionary<Vector3Int, List<StructureContext>>();
        private readonly List<StructureInfo> pendingStructureInfo = new List<StructureInfo>();

        public bool CheckInsideWorld(Vector3Int pos)
        {
            int offsetX = (Bounds.maxX+Bounds.minX)>>1;
            int offsetZ = (Bounds.maxZ+Bounds.minZ)>>1;

            int xx = (pos.x-offsetX)/Env.ChunkSize;
            int zz = (pos.z-offsetZ)/Env.ChunkSize;
            int yy = pos.y/Env.ChunkSize;
            int horizontalRadius = (Bounds.maxX-Bounds.minX)/(2*Env.ChunkSize);

            return xx*xx+zz*zz<=horizontalRadius*horizontalRadius &&
                   yy>=(Bounds.minY/Env.ChunkSize) && yy<=(Bounds.maxY/Env.ChunkSize);
        }

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

            textureProvider.Init(config);
            blockProvider.Init(config.blockFolder, this);

            foreach (var renderMaterial in renderMaterials)
            {
                renderMaterial.mainTexture = textureProvider.atlas;
            }
        }

        private void VerifyConfig()
        {
            // minX can't be greater then maxX
            if (config.minX > config.maxX)
            {
                int tmp = config.minX;
                config.maxX = config.minX;
                config.minX = tmp;
            }

            if (config.minX != config.maxX)
            {
                // Make sure there is at least one chunk worth of space in the world on the X axis
                if (config.maxX - config.minX < Env.ChunkSize)
                    config.maxX = config.minX + Env.ChunkSize;
            }

            // minY can't be greater then maxY
            if (config.minY > config.maxY)
            {
                int tmp = config.minY;
                config.maxY = config.minY;
                config.minY = tmp;
            }

            if (config.minY != config.maxY)
            {
                // Make sure there is at least one chunk worth of space in the world on the Y axis
                if (config.maxY - config.minY < Env.ChunkSize)
                    config.maxY = config.minY + Env.ChunkSize;
            }

            // minZ can't be greater then maxZ
            if (config.minZ>config.maxZ)
            {
                int tmp = config.minZ;
                config.maxZ = config.minZ;
                config.minZ = tmp;
            }

            if (config.minZ!=config.maxZ)
            {
                // Make sure there is at least one chunk worth of space in the world on the Z axis
                if (config.maxZ-config.minZ<Env.ChunkSize)
                    config.maxZ = config.minZ+Env.ChunkSize;
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

        public void CapCoordXInsideWorld(ref int minX, ref int maxX)
        {
            if (config.minX!=config.maxX)
            {
                minX = Mathf.Max(minX, config.minX);
                maxX = Mathf.Min(maxX, config.maxX);
            }
        }

        public void CapCoordYInsideWorld(ref int minY, ref int maxY)
        {
            if (config.minY!=config.maxY)
            {
                minY = Mathf.Max(minY, config.minY);
                maxY = Mathf.Min(maxY, config.maxY);
            }
        }

        public void CapCoordZInsideWorld(ref int minZ, ref int maxZ)
        {
            if (config.minZ!=config.maxZ)
            {
                minZ = Mathf.Max(minZ, config.minZ);
                maxZ = Mathf.Min(maxZ, config.maxZ);
            }
        }

        public bool IsCoordInsideWorld(ref Vector3Int pos)
        {
            return
                config.minX==config.maxX || (pos.x>=config.minX && pos.x<=config.maxX) ||
                config.minY==config.maxY || (pos.y>=config.minY && pos.y<=config.maxY) ||
                config.minZ==config.maxZ || (pos.z>=config.minZ && pos.z<=config.maxZ);
        }
        
        public void RegisterModifyRange(ModifyBlockContext onModified)
        {
            modifyRangeQueue.Add(onModified);
        }

        public void PerformBlockActions()
        {
            for (int i = 0; i<modifyRangeQueue.Count; i++)
                modifyRangeQueue[i].PerformAction();

            modifyRangeQueue.Clear();
        }

        public void RegisterPendingStructure(StructureInfo info, StructureContext context)
        {
            if (info==null || context==null)
                return;

            lock (pendingStructureMutex)
            {
                {
                    bool alreadyThere = false;

                    // Do not register the same thing twice
                    for (int i = 0; i<pendingStructureInfo.Count; i++)
                    {
                        if (pendingStructureInfo[i].Equals(info))
                        {
                            alreadyThere = true;
                            break;
                        }
                    }

                    if (!alreadyThere)
                        pendingStructureInfo.Add(info);
                }

                List<StructureContext> list;
                if (pendingStructures.TryGetValue(context.chunkPos, out list))
                    list.Add(context);
                else
                    pendingStructures.Add(context.chunkPos, new List<StructureContext> {context});
            }

            {
                Chunk chunk;
                lock (chunks)
                {
                    // Let the chunk know it needs an update if it exists
                    chunk = chunks.Get(ref context.chunkPos);
                }
                if (chunk != null)
                    chunk.NeedApplyStructure = true;
            }
        }

        public void UnregisterPendingStructures()
        {
            // TODO: This is not exactly optimal. A lot of iterations for one mutex. On the other hand, I expect only
            // a small amount of structures stored here. Definitelly not hundreds or more. But there's a room for
            // improvement...
            lock (pendingStructureMutex)
            {
                // Let's see whether we can unload any positions
                for (int i=0; i<pendingStructureInfo.Count;)
                {
                    var info = pendingStructureInfo[i];
                    var pos = info.chunkPos;

                    // See whether we can remove the structure
                    if (!Bounds.IsInside(ref pos))
                        pendingStructureInfo.RemoveAt(i);
                    else
                    {
                        ++i;
                        continue;
                    }

                    // Structure removed. We need to remove any associated world positions now
                    for (int y = info.bounds.minY; y<info.bounds.maxY; y += Env.ChunkSize)
                    {
                        for (int z = info.bounds.minZ; z<info.bounds.maxZ; z += Env.ChunkSize)
                        {
                            for (int x = info.bounds.minX; x<info.bounds.maxX; x += Env.ChunkSize)
                            {
                                List<StructureContext> list;
                                if (!pendingStructures.TryGetValue(new Vector3Int(x,y,z), out list) || list.Count <= 0)
                                    continue;

                                // Remove any occurence of this structure from pending positions
                                for (int j = 0; j<list.Count; )
                                {
                                    if (list[j].id==info.id)
                                        list.RemoveAt(j);
                                    else
                                        ++j;
                                }
                            }
                        }
                    }
                }
            }
        }

        public void ApplyPendingStructures(Chunk chunk)
        {
            // Check this unlocked first
            if (!chunk.NeedApplyStructure)
                return;

            List<StructureContext> list;
            int cnt;

            lock (pendingStructureMutex)
            {
                if (!chunk.NeedApplyStructure)
                    return;
                
                // Consume the event
                chunk.NeedApplyStructure = false;

                if (!pendingStructures.TryGetValue(chunk.pos, out list))
                    return;
                
                cnt = list.Count;
            }
            
            // Apply changes to the chunk
            for (int i = chunk.MaxPendingStructureListIndex; i<cnt; i++)
                list[i].Apply(chunk);
            chunk.MaxPendingStructureListIndex = cnt-1;
        }
    }
}
