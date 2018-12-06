using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities.Noise;

public class TerrainGen
{
    public TerrainLayer[] TerrainLayers { get; private set; }
    public TerrainLayer[] StructureLayers { get; private set; }

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
        // Verify all correct layers
        ProcessConfigs(world, layerFolder);
    }

    private void ProcessConfigs(World world, string layerFolder)
    {
        var configLoader = new ConfigLoader<LayerConfig>(new[] { layerFolder });

        List<LayerConfig> layersConfigs = new List<LayerConfig>(configLoader.AllConfigs());

        // Terrain layers
        List<TerrainLayer> terrainLayers = new List<TerrainLayer>();
        List<int> terrainLayersIndexes = new List<int>(); // could be implemented as a HashSet, however, it would be insane storing hundreads of layers here

        // Structure layers
        List<TerrainLayer> structLayers = new List<TerrainLayer>();
        List<int> structLayersIndexes = new List<int>(); // could be implemented as a HashSet, however, it would be insane storing hundreads of layers here

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

            if (layer.isStructure)
            {
                // Do not allow any two layers share the same index
                if (structLayersIndexes.Contains(layer.index))
                {
                    Debug.LogError("Could not create structure layer "+config.layerType+" : "+config.name+". Index "+
                                   layer.index.ToString()+" already defined");
                    layersConfigs.RemoveAt(i);
                    continue;
                }

                // Add layer to layers list
                structLayers.Add(layer);
                structLayersIndexes.Add(layer.index);
            }
            else
            {
                // Do not allow any two layers share the same index
                if (terrainLayersIndexes.Contains(layer.index))
                {
                    Debug.LogError("Could not create terrain layer "+config.layerType+" : "+config.name+". Index "+
                                   layer.index.ToString()+" already defined");
                    layersConfigs.RemoveAt(i);
                    continue;
                }

                // Add layer to layers list
                terrainLayers.Add(layer);
                terrainLayersIndexes.Add(layer.index);
            }

            ++i;
        }

        // Call OnInit for each layer now that they all have been set up. Thanks to this, layers can
        // e.g. address other layers knowing that they will be able to access all data they need.
        int ti = 0, si = 0;
        for (int i = 0; i<layersConfigs.Count; i++)
        {
            LayerConfig config = layersConfigs[i];
            if (LayerConfig.IsStructure(config.structure))
                structLayers[si++].Init(config);
            else
                terrainLayers[ti++].Init(config);
        }

        // Sort the layers by index
        TerrainLayers = terrainLayers.ToArray();
        Array.Sort(TerrainLayers);
        StructureLayers = structLayers.ToArray();
        Array.Sort(StructureLayers);

        // Register support for noise functionality with each workpool thread
        for (int i = 0; i < Globals.WorkPool.Size; i++)
        {
            var pool = Globals.WorkPool.GetPool(i);
            pool.noiseItems = new NoiseItem[layersConfigs.Count];
            for (int j = 0; j < layersConfigs.Count; j++)
            {
                pool.noiseItems[j] = new NoiseItem
                {
                    noiseGen = new NoiseInterpolator()
                };
            }
        }
    }

    public void GenerateTerrain(Chunk chunk)
    {
        // Do some layer preprocessing on a chunk
        for (int i=0; i<TerrainLayers.Length; i++)
            TerrainLayers[i].PreProcess(chunk, i);

        /* // DEBUG CODE
        for(int y=0; y<Env.ChunkSize; y++)
            for (int z = 0; z < Env.ChunkSize; z++)
                for (int x = 0; x<Env.ChunkSize; x++)
                {
                    int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                    chunk.blocks.SetRaw(index, new BlockData(4, true));
                }
        */
        // Generate terrain and structures
        GenerateTerrainForChunk(chunk);
        GenerateStructuresForChunk(chunk);
        
        // Do some layer postprocessing on a chunk
        for (int i=0; i<TerrainLayers.Length; i++)
            TerrainLayers[i].PostProcess(chunk, i);
    }

    /// <summary>
    /// Retrieves the terrain height in a given chunk on given coordinates
    /// </summary>
    /// <param name="chunk">Chunk for which terrain is generated</param>
    /// <param name="x">Position on the x axis in local coordinates</param>
    /// <param name="z">Position on the z axis in local coordinates</param>
    public float GetTerrainHeightForChunk(Chunk chunk, int x, int z)
    {
        float height = 0f;
        for (int i = 0; i < TerrainLayers.Length; i++)
            height = TerrainLayers[i].GetHeight(chunk, i, x, z, height, 1f);

        return height;
    }

    public float GetTerrainTemperatureForChunk(Chunk chunk, int x, int z)
    {
        float temp = 0f;
        for (int i = 0; i < TerrainLayers.Length; i++)
            temp = TerrainLayers[i].GetTemperature(chunk, i, x, z, temp);

        return temp;
    }

    public float GetTerrainHumidityForChunk(Chunk chunk, int x, int z)
    {
        float hum = 0f;
        for (int i = 0; i < TerrainLayers.Length; i++)
            hum = TerrainLayers[i].GetHumidity(chunk, i, x, z, hum);

        return hum;
    }

    /// <summary>
    /// Generates terrain for a given chunk
    /// </summary>
    /// <param name="chunk">Chunk for which terrain is generated</param>
    public void GenerateTerrainForChunk(Chunk chunk)
    {
        int maxY = chunk.Pos.y + Env.ChunkSize;
        for (int z = 0; z<Env.ChunkSize; z++)
        {
            for (int x = 0; x<Env.ChunkSize; x++)
            {
                float height = 0f;
                float temp = 0f;
                float hum = 0f;
                for (int i = 0; i<TerrainLayers.Length; i++)
                {
                    temp = TerrainLayers[i].GetTemperature(chunk, i, x, z, temp);
                    hum = TerrainLayers[i].GetHumidity(chunk, i, x, z, hum);
                    height = TerrainLayers[i].GenerateLayer(chunk, i, x, z, height, temp, hum);

                    // Note: We can do this if there are any substraction layers
                    if (height > maxY)
                        break;
                }
            }
        }
    }

    /// <summary>
    /// Generates structures for a given chunk
    /// </summary>
    /// <param name="chunk">Chunk for which structures are generated</param>
    public void GenerateStructuresForChunk(Chunk chunk)
    {
        for (int i = 0; i < StructureLayers.Length; i++)
            StructureLayers[i].GenerateStructures(chunk, i);
    }

}