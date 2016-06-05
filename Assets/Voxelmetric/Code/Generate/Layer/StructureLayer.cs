using System;
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

    public override void GenerateStructures(Chunk chunk)
    {
        int minX, maxX, minZ, maxZ;

        minX = chunk.pos.x - structure.negX;
        maxX = chunk.pos.x + Config.Env.ChunkSize + structure.posX;
        minZ = chunk.pos.z - structure.negZ;
        maxZ = chunk.pos.z + Config.Env.ChunkSize + structure.posZ;

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
                        int height = terrainGen.GenerateTerrainForBlockColumn(x, z, true, chunk);
                        structure.Build(world, chunk, new BlockPos(x, height, z), this);
                    }
                }
            }
        }
    }
}
