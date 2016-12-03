using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;

public class TerrainGen
{
    public TerrainLayer[] Layers { get; private set; }
    public Noise noise { get; private set; }

    public static TerrainGen Create(World world, string layerFolder)
    {
        TerrainGen provider = new TerrainGen();
        provider.Init(world, layerFolder);
        return provider;
    }

    protected TerrainGen()
    {
    }

    protected void Init(World world, string layerFolder)
    {
        noise = new Noise(world.name);

        // Verify all correct layers
        Layers = ProcessConfigs(world, layerFolder);

        // Sort the layers by index
        Array.Sort(Layers);
    }

    private TerrainLayer[] ProcessConfigs(World world, string layerFolder)
    {
        var configLoader = new ConfigLoader<LayerConfig>(new[] { layerFolder });

        List<LayerConfig> layersConfigs = new List<LayerConfig>(configLoader.AllConfigs());
        List<TerrainLayer> layers = new List<TerrainLayer>(layersConfigs.Count);
        HashSet<int> indexes = new HashSet<int>();

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

            // Do not allow any two layers share the same index
            if (indexes.Contains(layer.index))
            {
                Debug.LogError("Could not create layer " + config.layerType + " : " + config.name + ". Index " + layer.index + " already defined");
                layersConfigs.RemoveAt(i);
                continue;
            }

            // Add layer to layers list
            layers.Add(layer);
            indexes.Add(layer.index);

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

    public void GenerateTerrain(Chunk chunk)
    {
        for (int z = chunk.pos.z; z < chunk.pos.z + Env.ChunkSize; z++)
        {
            for (int x = chunk.pos.x; x < chunk.pos.x + Env.ChunkSize; x++)
            {
                GenerateTerrainForChunk(chunk, x, z);
            }
        }
        GenerateStructuresForChunk(chunk);
    }

    /// <summary>
    /// Retrieves the terrain height in a given chunk on given coordinates
    /// </summary>
    /// <param name="chunk">Chunk for which terrain is generated</param>
    /// <param name="x">Position on the x axis in world coordinates</param>
    /// <param name="z">Position on the z axis in world coordinates</param>
    public int GetTerrainHeightForChunk(Chunk chunk, int x, int z)
    {
        int height = 0;
        for (int i = 0; i < Layers.Length; i++)
        {
            TerrainLayer layer = Layers[i];
            if (layer == null)
            {
                Debug.LogError("Layer name '" + layer + "' in layer order didn't match a valid layer");
                continue;
            }

            if (!layer.isStructure)
                height = Layers[i].GetHeight(chunk, x, z, height, 1);
        }
        return height;
    }

    /// <summary>
    /// Generates terrain for a given chunk
    /// </summary>
    /// <param name="chunk">Chunk for which terrain is generated</param>
    /// <param name="x">Position on the x axis in world coordinates</param>
    /// <param name="z">Position on the z axis in world coordinates</param>
    public int GenerateTerrainForChunk(Chunk chunk, int x, int z)
    {
        int height = 0;
        for (int i = 0; i < Layers.Length; i++)
        {
            TerrainLayer layer = Layers[i];
            if (layer == null)
            {
                Debug.LogError("Layer name '" + layer + "' in layer order didn't match a valid layer");
                continue;
            }

            if (!layer.isStructure)
                height = Layers[i].GenerateLayer(chunk, x, z, height, 1);
        }
        return height;
    }

    /// <summary>
    /// Generates structures for a given chunk
    /// </summary>
    /// <param name="chunk">Chunk for which structures are generated</param>
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