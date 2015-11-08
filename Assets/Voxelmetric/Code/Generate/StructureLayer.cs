using System;
using SimplexNoise;
using UnityEngine;
public class StructureLayer : TerrainLayer
{
    protected GeneratedStructure structure;
    float chance;

    protected override void SetUp(LayerConfig config)
    {
        // Config files for random layers MUST define these properties
        chance = float.Parse(properties["chance"]);

        var structureType = Type.GetType(config.structure + ", " + typeof(GeneratedStructure).Assembly, false);
        if (structureType == null)
            Debug.LogError("Could not create structure " + config.structure);

        structure = (GeneratedStructure)Activator.CreateInstance(structureType);
    }

    public override void GenerateStructures(BlockPos chunkColumnPos)
    {
        int minX, maxX, minZ, maxZ;

        minX = chunkColumnPos.x - structure.negX;
        maxX = chunkColumnPos.x + Config.Env.ChunkSize + structure.posX;
        minZ = chunkColumnPos.z - structure.negZ;
        maxZ = chunkColumnPos.z + Config.Env.ChunkSize + structure.posZ;

        for (int x = minX; x < maxX; x++)
        {
            for (int z = minZ; z < maxZ; z++)
            {
                BlockPos pos = new BlockPos(x, 0, z);
                float chanceAtPos = pos.Random(44, true);

                if (chance > chanceAtPos)
                {
                    if (pos.Add(1, 0, 0).Random(44, true) > chanceAtPos
                        && pos.Add(-1, 0, 0).Random(44, true) > chanceAtPos
                        && pos.Add(0, 0, 1).Random(44, true) > chanceAtPos
                        && pos.Add(0, 0, -1).Random(44, true) > chanceAtPos)
                    {
                        int height = terrainGen.GenerateTerrainForBlockColumn(x, z, true);
                        structure.Build(world, chunkColumnPos, new BlockPos(x, height, z), this);
                    }
                }
            }
        }
    }
}
