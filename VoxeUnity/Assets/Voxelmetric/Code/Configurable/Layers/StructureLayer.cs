using System;
using Assets.Voxelmetric.Code.Common.Math;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources;
using Voxelmetric.Code.Utilities;

public class StructureLayer : TerrainLayer
{
    protected GeneratedStructure structure;
    private float chance;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for random layers MUST define these properties
        chance = float.Parse(properties["chance"]);

        var structureType = Type.GetType(config.structure + ", " + typeof(GeneratedStructure).Assembly, false);
        if (structureType == null)
            Debug.LogError("Could not create structure " + config.structure);

        structure = (GeneratedStructure)Activator.CreateInstance(structureType);
    }

    public override void Init(LayerConfig config)
    {
        structure.Init(world);
    }

    public override int GetHeight(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        return heightSoFar;
    }

    public override int GenerateLayer(Chunk chunk, int x, int z, int heightSoFar, float strength)
    {
        return heightSoFar;
    }

    public override void GenerateStructures(Chunk chunk)
    {
        int minX = chunk.pos.x - structure.negX;
        int maxX = chunk.pos.x + Env.ChunkSize + structure.posX;
        int minZ = chunk.pos.z - structure.negZ;
        int maxZ = chunk.pos.z + Env.ChunkSize + structure.posZ;

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                Vector3Int pos = new Vector3Int(x, 0, z);
                float chanceAtPos = Randomization.Random(pos.GetHashCode(), 44, true);

                if (chance > chanceAtPos)
                {
                    if (Randomization.Random(pos.Add(1, 0, 0).GetHashCode(),44, true) > chanceAtPos &&
                        Randomization.Random(pos.Add(-1, 0, 0).GetHashCode(),44, true) > chanceAtPos &&
                        Randomization.Random(pos.Add(0, 0, 1).GetHashCode(),44, true) > chanceAtPos &&
                        Randomization.Random(pos.Add(0, 0, -1).GetHashCode(),44, true) > chanceAtPos)
                    {
                        int height = terrainGen.GetTerrainHeightForChunk(chunk, x, z);
                        structure.Build(world, chunk, new Vector3Int(x, height, z), this);
                    }
                }
            }
        }
    }
}