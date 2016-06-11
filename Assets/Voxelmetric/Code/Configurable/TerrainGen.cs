using System;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;

public class TerrainGen
{
    public Noise noise;
    World world;
    TerrainLayer[] layers;

    public TerrainGen(World world, string layerFolder)
    {
        this.world = world;
        noise = new Noise(world.name);

        ConfigLoader<LayerConfig> layerConfigs = new ConfigLoader<LayerConfig>(new string[] { layerFolder });

        layers = new TerrainLayer[layerConfigs.AllConfigs().Length];
        for (int i = 0; i < layerConfigs.AllConfigs().Length; i++)
        {
            LayerConfig config = layerConfigs.AllConfigs()[i];
            var type = Type.GetType(config.layerType + ", " + typeof(Block).Assembly, false);
            if (type == null)
                Debug.LogError("Could not create layer " + config.layerType + " : " + config.name);

            layers[i] = (TerrainLayer)Activator.CreateInstance(type);
            layers[i].BaseSetUp(config, world, this);
        }
        
        //Sort the layers by index
        Array.Sort(layers);
    }
    
    public void GenerateTerrainForChunk(Chunk chunk)
    {
        for (int x = chunk.pos.x; x < chunk.pos.x + Env.ChunkSize; x++)
        {
            for (int z = chunk.pos.z; z < chunk.pos.z + Env.ChunkSize; z++)
            {
                GenerateTerrainForBlockColumn(x, z, false, chunk);
            }
        }
        GenerateStructuresForChunk(chunk);
    }

    public int GenerateTerrainForBlockColumn(int x, int z, bool justGetHeight, Chunk chunk)
    {
        int height = world.config.minY;
        for (int i = 0; i < layers.Length; i++)
        {
            TerrainLayer layer = layers[i];
            if (layer == null)
            {
                Debug.LogError("Layer name '" + layer + "' in layer order didn't match a valid layer");
                continue;
            }

            if (!layer.isStructure)
                height = layers[i].GenerateLayer(chunk, x, z, height, 1, justGetHeight);
        }
        return height;
    }

    public void GenerateStructuresForChunk(Chunk chunk)
    {
        for (int i = 0; i < layers.Length; i++)
        {
            TerrainLayer layer = layers[i];
            if (layer == null || !layer.isStructure)
                continue;

            layer.GenerateStructures(chunk);
        }
    }

}