using UnityEngine;
using System;
using System.Collections;
using SimplexNoise;

public class TerrainGen
{
    public TerrainGen(World world, string layerFolder)
    {
        this.world = world;
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

    World world;
    TerrainLayer[] layers;

    public void GenerateTerrainForChunkColumn(BlockPos pos)
    {
        Chunk[] chunks = new Chunk[(world.config.maxY - world.config.minY) / Config.Env.ChunkSize];
        for (int y = world.config.minY; y < world.config.maxY; y += Config.Env.ChunkSize)
        {
            chunks[(y - world.config.minY) / Config.Env.ChunkSize] = world.GetChunk(new BlockPos(pos.x, y, pos.z)); ;
        }

        for (int x = pos.x; x < pos.x + Config.Env.ChunkSize; x++)
        {
            for (int z = pos.z; z < pos.z + Config.Env.ChunkSize; z++)
            {
                GenerateTerrainForBlockColumn(x, z, false, chunks);
            }
        }

        GenerateStructuresForChunk(pos);
    }

    public int GenerateTerrainForBlockColumn(int x, int z, bool justGetHeight, Chunk[] chunks = null)
    {
        int height = world.config.minY;
        for (int i = 0; i < layers.Length; i++)
        {
            if (layers[i] == null)
            {
                Debug.LogError("Layer name '" + layers[i] + "' in layer order didn't match a valid layer");
                continue;
            }

            if (!layers[i].isStructure)
            {
                height = layers[i].GenerateLayer(chunks, x, z, height, 1, justGetHeight);
            }
        }
        return height;
    }

    public void GenerateStructuresForChunk(BlockPos chunkPos)
    {
        for (int i = 0; i < layers.Length; i++)
        {

            if (layers[i] == null)
                continue;

            if (layers[i].isStructure)
            {
                layers[i].GenerateStructures(chunkPos);
            }
        }
    }

}


