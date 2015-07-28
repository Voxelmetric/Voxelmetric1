using SimplexNoise;
using System.Collections;
using UnityEngine;
using System.Reflection;
using System;

public class TerrainLayer: MonoBehaviour {

    public enum LayerType { Absolute, Additive, Surface, Structure };

    public LayerType layerType = LayerType.Absolute;
    public string layerName = "layername";

    [Header("Absolute and additive layer parameters")]
    [Range(0, 256)]
    [Tooltip("Minimum height of this layer")]
    public int baseHeight = 0;
    [Range(1, 256)]
    [Tooltip("Distance between peaks")]
    public int frequency = 10;
    [Range(1, 256)]
    [Tooltip("The max height of peaks")]
    public int amplitude = 10;
    [Range(1, 3)]
    [Tooltip("Applies the height to the power of this value")]
    public float exponent = 1;

    public string blockName = "stone";

    [Header("Structure parameters")]
    [Range(0, 100)]
    [Tooltip("The threshold to create the structure at a location, if you get tightly packed clusters raise this")]
    public int percentage=90;
    [Range(1, 256)]
    [Tooltip("Distance between structures")]
    public int structureFrequency = 10;
    [Tooltip("Name of the class that generates this structure")]
    public string structureClassName = "StructureTree";

    public bool customTerrainLayer = false;
    public string terrainLayerClassName = "";
    public LayerOverride customLayer;

    public GeneratedStructure structure;

    public World world;
    public Noise noiseGen;


    public TerrainLayer SetUpTerrainLayer(World world, Noise noise)
    {
        this.world = world;
        this.noiseGen = noise;
        if (layerType == LayerType.Structure)
        {
            var type = Type.GetType(structureClassName + ", " + typeof(GeneratedStructure).Assembly);
            structure = (GeneratedStructure)Activator.CreateInstance(type);
        }

        return this;
    }

    public virtual int GenerateLayer(int x, int z, int heightSoFar, float strength, bool justGetHeight = false)
    {
        if (layerType == LayerType.Structure)
            return heightSoFar;

        if (customTerrainLayer)
        {
            //Not the best place for this but it ensures that custom layers are always initialized before use
            if (customLayer == null)
            {
                var customLayerType = Type.GetType(terrainLayerClassName + ", " + typeof(LayerOverride).Assembly);
                customLayer = (LayerOverride)Activator.CreateInstance(customLayerType);

                customLayer.world = world;
                customLayer.noiseGen = noiseGen;
                customLayer.baseHeight = baseHeight;
                customLayer.frequency = frequency;
                customLayer.amplitude = amplitude;
                customLayer.exponent = exponent;
                customLayer.blockName = blockName;
                customLayer.percentage = percentage;
                customLayer.structureFrequency = structureFrequency;

                if (layerType == LayerType.Structure)
                    customLayer.structure = structure;
            }

            int newHeight = customLayer.GenerateLayer(x, z, heightSoFar, strength, justGetHeight);
            return newHeight;
        }

        Block blockToPlace = blockName;
        blockToPlace.modified = false;

        int height = GetNoise(x, 0, z, frequency, amplitude, exponent);
        height += baseHeight;
        height = (int)(height * strength);

        if (!justGetHeight)
        {
            if (layerType == LayerType.Absolute)
            {
                for (int y = heightSoFar; y < height + Config.Env.WorldMinY; y++)
                {
                    world.SetBlock(new BlockPos(x, y, z), blockToPlace, false);
                }
            }
            else //additive or surface
            {
                for (int y = heightSoFar; y < height + heightSoFar; y++)
                {
                    world.SetBlock(new BlockPos(x, y, z), blockToPlace, false);
                }
            }
        }

        if (layerType == LayerType.Additive || layerType == LayerType.Surface)
        {
            return heightSoFar + height;
        }
        else //absolute
        {
            if (Config.Env.WorldMinY + height > heightSoFar)
                return Config.Env.WorldMinY + height;
        }
        return heightSoFar;
    }

    public virtual void GenerateStructures(BlockPos chunkPos, TerrainGen terrainGen)
    {
        if (layerType != LayerType.Structure)
            return;

        if (customTerrainLayer)
        {
            customLayer.GenerateStructures(chunkPos, terrainGen);
            return;
        }

        int minX = chunkPos.x - structure.negX;
        int maxX = chunkPos.x + Config.Env.ChunkSize + structure.posX;
        int minZ = chunkPos.z - structure.negZ;
        int maxZ = chunkPos.z + Config.Env.ChunkSize + structure.posZ;

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                int percentChance = GetNoise(x, 0, z, structureFrequency, 100, 1);
                if (GetNoise(x, 0, z, structureFrequency, 100, 1) > percentage)
                {
                    if (percentChance > GetNoise(x + 1, 0, z, structureFrequency, 100, 1)
                        && percentChance > GetNoise(x - 1, 0, z, structureFrequency, 100, 1)
                        && percentChance > GetNoise(x, 0, z + 1, structureFrequency, 100, 1)
                        && percentChance > GetNoise(x, 0, z - 1, structureFrequency, 100, 1))
                    {

                        int height = terrainGen.GenerateTerrainForBlockColumn(x, z, true);
                        structure.Build(world, chunkPos, new BlockPos(x, height, z), this);
                    }
                }
            }
        }

    }

    public int GetNoise(int x, int y, int z, float scale, int max, float power)
    {
        float noise = (noiseGen.Generate(x / scale, y / scale, z / scale) + 1f) * (max / 2f);

        noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }

}
