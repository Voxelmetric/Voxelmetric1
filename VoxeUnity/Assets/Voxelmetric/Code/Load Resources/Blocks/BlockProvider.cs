using System;
using System.Collections;
using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Load_Resources.Blocks
{
    public class BlockProvider
    {
        //! Air type block will always be present
        public static readonly ushort AirType = 0;

        private readonly List<BlockConfig> m_configs;
        private readonly Dictionary<string, ushort> m_names;
        //! Mapping from typeInConfig to type
        private readonly Dictionary<ushort, ushort> m_types;

        public Block[] BlockTypes { get; private set; }

        public static BlockProvider Create(string blockFolder, World world)
        {
            BlockProvider provider = new BlockProvider();
            provider.Init(blockFolder, world);
            return provider;
        }

        private BlockProvider()
        {
            m_names = new Dictionary<string, ushort>();
            m_types = new Dictionary<ushort, ushort>();
            m_configs = new List<BlockConfig>();
        }

        private void Init(string blockFolder, World world)
        {
            // Add the static air block type
            AddBlockType(BlockConfig.CreateAirBlockConfig(world));

            // Add all the block definitions defined in the config files
            ProcessConfigs(world, blockFolder);

            // Build block type lookup table
            BlockTypes = new Block[m_configs.Count];
            for (int i = 0; i< m_configs.Count; i++)
            {
                BlockConfig config = m_configs[i];

                Block block = (Block)Activator.CreateInstance(config.blockClass);
                block.type = (ushort)i;
                block.config = config;
                BlockTypes[i] = block;
            }

            // Once all blocks are set up, call OnInit on them. It is necessary to do it in a separate loop
            // in order to ensure there will be no dependency issues.
            for (int i = 0; i < BlockTypes.Length; i++)
            {
                Block block = BlockTypes[i];
                block.OnInit(this);
            }
        }

        // World is only needed for setting up the textures
        private void ProcessConfigs(World world, string blockFolder)
        {
            var configFiles = Resources.LoadAll<TextAsset>(blockFolder);
            foreach (var configFile in configFiles)
            {
                Hashtable configHash = JsonConvert.DeserializeObject<Hashtable>(configFile.text);

                Type configType = Type.GetType(configHash["configClass"] + ", " + typeof(BlockConfig).Assembly, false);
                if (configType == null)
                {
                    Debug.LogError("Could not create config for " + configHash["configClass"]);
                    continue;
                }

                BlockConfig config = (BlockConfig)Activator.CreateInstance(configType);
                if (!config.SetUp(configHash, world))
                    continue;

                if (!VerifyBlockConfig(config))
                    continue;

                AddBlockType(config);
            }
        }

        private bool VerifyBlockConfig(BlockConfig config)
        {
            // Unique identifier of block type
            if (m_names.ContainsKey(config.name))
            {
                Debug.LogErrorFormat("Two blocks with the name {0} are defined", config.name);
                return false;
            }

            // Unique identifier of block type
            if (m_types.ContainsKey(config.typeInConfig))
            {
                Debug.LogErrorFormat("Two blocks with type {0} are defined", config.typeInConfig);
                return false;
            }

            // Class name must be valid
            if (config.blockClass == null)
            {
                Debug.LogErrorFormat("Invalid class name {0} for block {1}", config.className, config.name);
                return false;
            }

            // Use the type defined in the config if there is one, otherwise add one to the largest index so far
            if (config.type == ushort.MaxValue)
            {
                Debug.LogError("Maximum number of block types reached for " + config.name);
                return false;
            }

            return true;
        }

        /// <summary>
        /// Adds a block type to the index and adds it's name to a dictionary for quick lookup
        /// </summary>
        /// <param name="config">The controller object for this block</param>
        /// <returns>The index of the block</returns>
        private void AddBlockType(BlockConfig config)
        {
            config.type = (ushort)m_configs.Count;
            m_configs.Add(config);
            m_names.Add(config.name, config.type);
            m_types.Add(config.typeInConfig, config.type);
        }

        public ushort GetType(string name)
        {
            ushort type;
            if (m_names.TryGetValue(name, out type))
                return type;

            Debug.LogError("Block not found: " + name);
            return AirType;
        }

        public ushort GetTypeFromTypeInConfig(ushort typeInConfig)
        {
            ushort type;
            if (m_types.TryGetValue(typeInConfig, out type))
                return type;

            Debug.LogError("TypeInConfig not found: " + typeInConfig);
            return AirType;
        }

        public Block GetBlock(string name)
        {
            ushort type;
            if (m_names.TryGetValue(name, out type))
                return BlockTypes[type];

            Debug.LogError("Block not found: " + name);
            return BlockTypes[AirType];
        }

        public BlockConfig GetConfig(ushort type)
        {
            if (type<m_configs.Count)
                return m_configs[type];

            Debug.LogError("Config not found: "+type);
            return m_configs[AirType];
        }
    }
}
