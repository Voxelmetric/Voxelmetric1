using SimplexNoise;
using System.Collections.Generic;
using UnityEngine;
using System.Collections;
using System;

public class TerrainLayer : IComparable
{

    public void BaseSetUp(LayerConfig config, World world, TerrainGen terrainGen)
    {
        this.terrainGen = terrainGen;
        layerName = config.name;
        isStructure = config.structure != null;
        this.world = world;
        noiseGen = world.noise;
        chunksPerColumn = (world.config.maxY - world.config.minY) / Config.Env.ChunkSize;
        index = config.index;

        foreach (var key in config.properties.Keys)
        {
            properties.Add(key.ToString(), config.properties[key].ToString());
        }

        SetUp(config);
    }

    protected virtual void SetUp(LayerConfig config) { }

    protected TerrainGen terrainGen;
    protected Dictionary<string, string> properties = new Dictionary<string, string>();
    public string layerName = "";
    public int index;
    public bool isStructure;
    protected World world;
    protected Noise noiseGen;
    protected int chunksPerColumn;

    public virtual int GenerateLayer(Chunk[] chunks, int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        return heightSoFar;
    }

    int temp(int x, int z, int heightSoFar, float strength, World world, Noise noise, bool justGetHeight = false)
    {
        return 0;
    }

    /// <summary>
    /// Called once for each chunk column. Should generate any 
    /// parts of the structure within the chunk using GeneratedStructure.
    /// </summary>
    /// <param name="chunkPos">pos of the chunk column</param>
    public virtual void GenerateStructures(BlockPos chunkPos)
    {

    }

    public int GetNoise(int x, int y, int z, float scale, int max, float power)
    {
        float noise = (noiseGen.Generate(x / scale, y / scale, z / scale) + 1f);
        noise *= (max / 2f);

        if(power!=1)
            noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }

    //Sets a column of chunks starting at startPlaceHeight and ending at endPlaceHeight using localSetBlock for speed
    protected void SetBlocksColumn(Chunk[] chunks, int x, int z, int startPlaceHeight, int endPlaceHeight, Block blockToPlace)
    {
        // Loop through each chunk in the column
        for (int i = 0; i < chunksPerColumn; i++)
        {
            // And for each one loop through its height
            for (int y = 0; y < Config.Env.ChunkSize; y++)
            {
                //and if this is above the starting height
                if (chunks[i].pos.y + y >= startPlaceHeight)
                {
                    // And below or equal to the end height place the block using 
                    // localSetBlock which is faster than the non-local pos set block
                    if (chunks[i].pos.y + y < endPlaceHeight)
                    {
                        //chunks[i].world.SetBlock(new BlockPos(x, y + chunks[i].pos.y, z), blockToPlace, false, false);
                        chunks[i].LocalSetBlock(new BlockPos(x - chunks[i].pos.x, y, z - chunks[i].pos.z), blockToPlace);
                    }
                    else
                    {
                        // Return early, we've reached the end of the blocks to add
                        return;
                    }
                }
            }
        }
    }

    // implement IComparable interface
    public int CompareTo(object obj)
    {
        return index.CompareTo((obj as TerrainLayer).index);
    }

}
