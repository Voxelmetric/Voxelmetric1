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

    public void GenerateTerrainForChunkColumn(BlockPos pos)
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