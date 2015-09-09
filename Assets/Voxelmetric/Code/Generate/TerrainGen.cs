using UnityEngine;
using System.Collections.Generic;
using SimplexNoise;

public class TerrainGen: MonoBehaviour
{
    public Noise noiseGen;
    public World world;

    public int temperatureScale = 100;
    public int drainageScale = 100;
    public int elevationScale = 100;
    public int rainfallScale = 100;

    public float percentageBiomePadding= 0.1f;

    public TerrainLayer[] layerOrder = new TerrainLayer[0];
    public OldTerrainGen oldTerrainGen;

    public void GenerateTerrainForChunkColumn(BlockPos pos)
    {
        if (Config.Toggle.UseOldTerrainGen)
        {
            if (oldTerrainGen == null)
                oldTerrainGen = new OldTerrainGen(noiseGen);

            for (int y = Config.Env.WorldMinY; y < Config.Env.WorldMaxY; y += Config.Env.ChunkSize)
            {
                Chunk chunk = world.GetChunk(pos.Add(0, y, 0));

                if(chunk != null)
                    oldTerrainGen.ChunkGen(chunk);

                for (int x = 0; x < Config.Env.ChunkSize; x++)
                {
                    for (int z = 0; z < Config.Env.ChunkSize; z++)
                    {

                    }
                }
            }
        }
        else
        {
            for (int x = pos.x; x < pos.x + Config.Env.ChunkSize; x++)
            {
                for (int z = pos.z; z < pos.z + Config.Env.ChunkSize; z++)
                {
                    GenerateTerrainForBlockColumn(x, z);
                }
            }

            GenerateStructuresForChunk(pos);
        }
    }

    public int GenerateTerrainForBlockColumn(int x, int z, bool justGetHeight = false)
    {
        int height = Config.Env.WorldMinY;
        for (int i = 0; i < layerOrder.Length; i++)
        {
            if (layerOrder[i] == null)
            {
                Debug.LogError("Layer name '" + layerOrder[i] + "' in layer order didn't match a valid layer");
                continue;
            }

            if (layerOrder[i].noiseGen == null)
            {
                layerOrder[i].SetUpTerrainLayer(world, noiseGen);
            }

            if (layerOrder[i].layerType != TerrainLayer.LayerType.Structure)
            {
                height = layerOrder[i].GenerateLayer(x, z, height, 1, justGetHeight);
            }
        }
        return height;
    }

    public void GenerateStructuresForChunk(BlockPos chunkPos)
    {
        for (int i = 0; i < layerOrder.Length; i++)
        {

            if (layerOrder[i] == null)
                continue;

            if (layerOrder[i].layerType == TerrainLayer.LayerType.Structure)
            {
                layerOrder[i].GenerateStructures(chunkPos, this);
            }
        }
    }

}