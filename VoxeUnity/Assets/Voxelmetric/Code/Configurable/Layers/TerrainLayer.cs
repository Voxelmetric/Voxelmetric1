using System;
using System.Collections.Generic;
using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities.Noise;

public abstract class TerrainLayer : IComparable, IEquatable<TerrainLayer>
{
    protected World world;
    protected TerrainGen terrainGen;
    protected readonly Dictionary<string, string> properties = new Dictionary<string, string>();
    protected NoiseWrapper noise;
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
    protected NoiseWrapperSIMD noiseSIMD;
#endif

    public string layerName = "";
    public int index { get; private set; }
    public bool isStructure { get; private set; }

    public NoiseWrapper Noise { get { return noise; } }
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
    public NoiseWrapperSIMD NoiseSIMD { get {return noiseSIMD;} }
#endif

    public void BaseSetUp(LayerConfig config, World world, TerrainGen terrainGen)
    {
        this.terrainGen = terrainGen;
        layerName = config.name;
        isStructure = LayerConfig.IsStructure(config.structure);
        this.world = world;
        index = config.index;

        noise = new NoiseWrapper(world.name);
#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
        noiseSIMD = new NoiseWrapperSIMD(world.name);
#endif

        foreach (var key in config.properties.Keys)
        {
            properties.Add(key.ToString(), config.properties[key].ToString());
        }

        SetUp(config);
    }

    protected virtual void SetUp(LayerConfig config) { }

    public virtual void Init(LayerConfig config) { }

    public virtual void PreProcess(Chunk chunk, int layerIndex) { }
    public virtual void PostProcess(Chunk chunk, int layerIndex) { }

    /// <summary>
    /// Retrieves the height on given coordinates
    /// </summary>
    /// <param name="chunk">Chunk for which we search for height</param>
    /// <param name="layerIndex">Index of layer generating this structure</param>
    /// <param name="x">Position on the x-axis in local coordinates</param>
    /// <param name="z">Position on the z-axis in local coordinates</param>
    /// <param name="heightSoFar">Position on the y-axis in world coordinates</param>
    /// <param name="strength">How much features are pronounced</param>
    /// <returns>List of chunks waiting to be saved.</returns>
    public abstract float GetHeight(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float strength);

    public abstract float GetTemperature(Chunk chunk, int layerIndex, int x, int z, float tempSoFar);

    public abstract float GetHumidity(Chunk chunk, int layerIndex, int x, int z, float humSoFar);

    /// <summary>
    /// Retrieves the height on given coordinates and if possible, updates the block within chunk based on the layer's configuration
    /// </summary>
    /// <param name="chunk">Chunk for which we search for height</param>
    /// <param name="layerIndex">Index of layer generating this structure</param>
    /// <param name="x">Position on the x-axis in local coordinates</param>
    /// <param name="z">Position on the z-axis in local coordinates</param>
    /// <param name="heightSoFar">Position on the y-axis in world coordinates</param>
    /// <param name="strength">How much features are pronounced</param>
    /// <returns>List of chunks waiting to be saved.</returns>
    public abstract float GenerateLayer(Chunk chunk, int layerIndex, int x, int z, float heightSoFar, float tempSoFar, float humSoFar, float strength);

    /// <summary>
    /// Called once for each chunk. Should generate any
    /// parts of the structure within the chunk using GeneratedStructure.
    /// </summary>
    /// <param name="chunk">Chunk for which structures are to be generated</param>
    /// <param name="layerIndex">Index of layer generating this structure</param>
    public virtual void GenerateStructures(Chunk chunk, int layerIndex)
    {
    }

    /// <summary>
    /// Fills chunk with layer data starting at startPlaceHeight and ending at endPlaceHeight
    /// </summary>
    /// <param name="chunk">Chunk filled with data</param>
    /// <param name="x">Position on x axis in local coordinates</param>
    /// <param name="z">Position on z axis in local coordinates</param>
    /// <param name="startPlaceHeight">Starting position on y axis in world coordinates</param>
    /// <param name="endPlaceHeight">Ending position on y axis in world coordinates</param>
    /// <param name="blockData">Block data to set</param>
    protected static void SetBlocks(Chunk chunk, int x, int z, int startPlaceHeight, int endPlaceHeight, BlockData blockData)
    {
        int chunkY = chunk.Pos.y;
        int chunkYMax = chunkY + Env.ChunkSize;
                
        int y = startPlaceHeight>chunkY ? startPlaceHeight : chunkY;        
        int yMax = endPlaceHeight<chunkYMax ? endPlaceHeight : chunkYMax;
        
        ChunkBlocks blocks = chunk.Blocks;
        int index = Helpers.GetChunkIndex1DFrom3D(x, y-chunkY, z);
        while (y++<yMax)
        {
            blocks.SetRaw(index, blockData);
            index += Env.ChunkSizeWithPaddingPow2;
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