using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;

public class TerrainGen
{
    private World world;
    private List<LayerConfig> m_configs;

    public TerrainLayer[] Layers { get; private set; }
    public Noise noise { get; private set; }

    public static TerrainGen Create(World world, string layerFolder)
    {
        TerrainGen provider = new TerrainGen();
        provider.Init(world, layerFolder);
        return provider;
    }

    private TerrainGen()
    {
        m_configs = new List<LayerConfig>();
    }

    public void Init(World world, string layerFolder)
    {
        this.world = world;
        noise = new Noise(world.name);

        // Verify all correct layers
        Layers = ProcessConfigs(world, layerFolder);

        //Sort the layers by index
        Array.Sort(Layers);
    }

    private TerrainLayer[] ProcessConfigs(World world, string layerFolder)
    {
        var configLoader = new ConfigLoader<LayerConfig>(new[] { layerFolder });

        List<LayerConfig> layersConfigs = new List<LayerConfig>(configLoader.AllConfigs());
        List<TerrainLayer> layers = new List<TerrainLayer>(layersConfigs.Count);

        for (int i = 0; i<layersConfigs.Count;)
        {
            LayerConfig config = layersConfigs[i];

            // Ignore broken configs
            var type = Type.GetType(config.layerType+", "+typeof (Block).Assembly, false);
            if (type==null)
            {
                Debug.LogError("Could not create layer "+config.layerType+" : "+config.name);
                layersConfigs.RemoveAt(i);
                continue;
            }

            // Set layers up
            TerrainLayer layer = (TerrainLayer)Activator.CreateInstance(type);
            layer.BaseSetUp(config, world, this);

            // Add layer to layers list
            layers.Add(layer);

            ++i;
        }

        // Call OnInit for each layer now that they all have been set up. Thanks to this, layers can
        // e.g. address other layers knowing that they will be able to access all data they need.
        for (int i = 0; i < layersConfigs.Count; i++)
        {
            LayerConfig config = layersConfigs[i];
            layers[i].Init(config);
        }

        return layers.ToArray();
    }

    public void GenerateTerrainForChunk(Chunk chunk)
    {
        for (int x = chunk.pos.x; x < chunk.pos.x + Env.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Env.ChunkSize; z++)
            {
                GenerateTerrainForBlockColumn(chunk, x, z, false);
            }
        }
        GenerateStructuresForChunk(chunk);
    }

    public int GenerateTerrainForBlockColumn(Chunk chunk, int x, int z, bool justGetHeight)
    {
        int height = world.config.minY;
        for (int i = 0; i < Layers.Length; i++)
        {
            TerrainLayer layer = Layers[i];
            if (layer == null)
            {
                Debug.LogError("Layer name '" + layer + "' in layer order didn't match a valid layer");
                continue;
            }

            if (!layer.isStructure)
                height = Layers[i].GenerateLayer(chunk, x, z, height, 1, justGetHeight);
        }
        return height;
    }

    public void GenerateStructuresForChunk(Chunk chunk)
    {
        for (int i = 0; i < Layers.Length; i++)
        {
            TerrainLayer layer = Layers[i];
            if (layer == null || !layer.isStructure)
                continue;

            layer.GenerateStructures(chunk);
        }
    }

}