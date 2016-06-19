using System;
using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;

public class TerrainLayer : IComparable, IEquatable<TerrainLayer>
{
    protected World world;
    protected Noise noiseGen;
    protected TerrainGen terrainGen;
    protected Dictionary<string, string> properties = new Dictionary<string, string>();

    public string layerName = "";
    public int index { get; private set; }
    protected int chunksPerColumn { get; private set; }
    public bool isStructure { get; private set; }

    public void BaseSetUp(LayerConfig config, World world, TerrainGen terrainGen)
    {
        this.terrainGen = terrainGen;
        layerName = config.name;
        isStructure = config.structure != null;
        this.world = world;
        noiseGen = terrainGen.noise;
        chunksPerColumn = (world.config.maxY - world.config.minY) / Env.ChunkSize;
        index = config.index;

        foreach (var key in config.properties.Keys)
        {
            properties.Add(key.ToString(), config.properties[key].ToString());
        }

        SetUp(config);
    }    

    protected virtual void SetUp(LayerConfig config) { }

    public virtual void Init(LayerConfig config) { }

    public virtual int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        return heightSoFar;
    }

    /// <summary>
    /// Called once for each chunk column. Should generate any 
    /// parts of the structure within the chunk using GeneratedStructure.
    /// </summary>
    /// <param name="chunkPos">pos of the chunk column</param>
    public virtual void GenerateStructures(Chunk chunk)
    {

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

    //Sets a column of chunks starting at startPlaceHeight and ending at endPlaceHeight using localSetBlock for speed
    protected static void SetBlocksColumn(Chunk chunk, int x, int z, int startPlaceHeight, int endPlaceHeight, Block blockToPlace)
    {
        if (startPlaceHeight >= chunk.pos.y + Env.ChunkSize || endPlaceHeight < chunk.pos.y)
            return;

        int y = startPlaceHeight;
        if (startPlaceHeight < chunk.pos.y)
            y = chunk.pos.y;

        while (y < chunk.pos.y + Env.ChunkSize && y < endPlaceHeight)
        {
            chunk.world.blocks.Set(new BlockPos(x, y, z), new BlockData(blockToPlace.type));
            y++;
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