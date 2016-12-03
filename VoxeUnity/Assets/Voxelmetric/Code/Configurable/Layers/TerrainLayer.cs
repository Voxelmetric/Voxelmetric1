using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;

public abstract class TerrainLayer : IComparable, IEquatable<TerrainLayer>
{
    protected World world;
    protected Noise noiseGen;
    protected TerrainGen terrainGen;
    protected Dictionary<string, string> properties = new Dictionary<string, string>();

    public string layerName = "";
    public int index { get; private set; }
    public bool isStructure { get; private set; }

    public void BaseSetUp(LayerConfig config, World world, TerrainGen terrainGen)
    {
        this.terrainGen = terrainGen;
        layerName = config.name;
        isStructure = config.structure != null;
        this.world = world;
        noiseGen = terrainGen.noise;
        index = config.index;

        foreach (var key in config.properties.Keys)
        {
            properties.Add(key.ToString(), config.properties[key].ToString());
        }

        SetUp(config);
    }

    protected virtual void SetUp(LayerConfig config) { }

    public virtual void Init(LayerConfig config) { }

    /// <summary>
    /// Retrieves the height on given coordinates
    /// </summary>
    /// <param name="chunk">Chunk for which we search for height</param>
    /// <param name="x">Position on the x-axis in world coordinates</param>
    /// <param name="z">Position on the z-axis in world coordinates</param>
    /// <param name="heightSoFar">Position on the y-axis in world coordinates</param>
    /// <param name="strength">How much features are pronounced</param>
    /// <returns>List of chunks waiting to be saved.</returns>
    public abstract int GetHeight(Chunk chunk, int x, int z, int heightSoFar, float strength);

    /// <summary>
    /// Retrieves the height on given coordinates and if possible, updates the block within chunk based on the layer's configuration
    /// </summary>
    /// <param name="chunk">Chunk for which we search for height</param>
    /// <param name="x">Position on the x-axis in world coordinates</param>
    /// <param name="z">Position on the z-axis in world coordinates</param>
    /// <param name="heightSoFar">Position on the y-axis in world coordinates</param>
    /// <param name="strength">How much features are pronounced</param>
    /// <returns>List of chunks waiting to be saved.</returns>
    public abstract int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength);

    /// <summary>
    /// Called once for each chunk. Should generate any
    /// parts of the structure within the chunk using GeneratedStructure.
    /// </summary>
    /// <param name="chunk">Chunk for which structures are to be generated</param>
    public virtual void GenerateStructures(Chunk chunk)
    {
    }

    public int GetNoise(int x, float scale, int max, float power)
    {
        float scaleInv = 1.0f/scale;
        float noise = (noiseGen.Generate(x*scaleInv)+1f);
        noise *= (max>>1);

        if (Math.Abs(power-1)>float.Epsilon)
            noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }

    public int GetNoise(int x, int y, float scale, int max, float power)
    {
        float scaleInv = 1.0f/scale;
        float noise = (noiseGen.Generate(x*scaleInv, y*scaleInv)+1f);
        noise *= (max>>1);

        if (Math.Abs(power-1)>float.Epsilon)
            noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }

    public int GetNoise(int x, int y, int z, float scale, int max, float power)
    {
        float scaleInv = 1.0f/scale;
        float noise = (noiseGen.Generate(x*scaleInv, y*scaleInv, z*scaleInv)+1f);
        noise *= (max>>1);

        if (Math.Abs(power-1)>float.Epsilon)
            noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }

    /// <summary>
    /// Fills chunk with layer data starting at startPlaceHeight and ending at endPlaceHeight
    /// </summary>
    /// <param name="chunk">Chunk filled with data</param>
    /// <param name="x">Position on x axis in world coordinates</param>
    /// <param name="z">Position on z axis in world coordinates</param>
    /// <param name="startPlaceHeight">Starting position on y axis in world coordinates</param>
    /// <param name="endPlaceHeight">Ending position on y axis in world coordinates</param>
    /// <param name="blockToPlace">Block type to set</param>
    protected static void SetBlocks(Chunk chunk, int x, int z, int startPlaceHeight, int endPlaceHeight, Block blockToPlace)
    {
        int yMax = chunk.pos.y+Env.ChunkSize;
        if (startPlaceHeight >= yMax || endPlaceHeight < chunk.pos.y)
            return;

        if (endPlaceHeight < yMax)
            yMax = endPlaceHeight;
        int y = startPlaceHeight;
        if (startPlaceHeight < chunk.pos.y)
            y = chunk.pos.y;

        while (y<yMax)
        {
            chunk.blocks.Set(new Vector3Int(x-chunk.pos.x, y-chunk.pos.y, z-chunk.pos.z), new BlockData(blockToPlace.type));
            ++y;
        }
    }

    #region Object-level comparison

    public int CompareTo(object obj)
    {
        return index.CompareTo(((TerrainLayer)obj).index);
    }
    public override bool Equals(object obj)
    {
        if (!(obj is TerrainLayer))
            return false;
        TerrainLayer other = (TerrainLayer)obj;
        return Equals(other);
    }

    public override int GetHashCode()
    {
        return index.GetHashCode();
    }

    public bool Equals(TerrainLayer other)
    {
        return other.index==index;
    }

    #endregion
}