using UnityEngine;
using System.Collections;
using SimplexNoise;

public class OldTerrainGen
{

    public OldTerrainGen(Noise noise)
    {
        noiseGen = noise;
        treeStructure = new StructureTree();
    }

    Noise noiseGen;
    TerrainGen terrainGen;

    protected int stoneBaseHeight = -20;
    protected float stoneBaseNoise = 0.03f;
    protected int stoneBaseNoiseHeight = 10;

    protected int stoneMountainHeight = 10;
    protected float stoneMountainFrequency = 0.008f;
    protected int stoneMinHeight = 0;

    protected int dirtBaseHeight = 1;
    protected float dirtNoise = 0.04f;
    protected int dirtNoiseHeight = 2;

    StructureTree treeStructure;

    public virtual void ChunkGen(Chunk chunk)
    {


        for (int x = 0; x < Config.Env.ChunkSize; x++)
        {
            for (int z = 0; z < Config.Env.ChunkSize; z++)
            {
                GenerateTerrain(chunk, x, z);
            }
        }

        for (int x = -3; x < Config.Env.ChunkSize + 3; x++)
        {
            for (int z = -3; z < Config.Env.ChunkSize + 3; z++)
            {
                CreateTreeIfValid(x, z, chunk);
            }
        }

    }

    protected virtual int LayerStoneBase(int x, int z)
    {
        int stoneHeight = stoneBaseHeight;
        stoneHeight += GetNoise(x, 0, z, stoneMountainFrequency, stoneMountainHeight, 1.6f);
        stoneHeight += GetNoise(x, 1000, z, 0.03f, 8, 1) * 2;

        if (stoneHeight < stoneMinHeight)
            return stoneMinHeight;

        return stoneHeight;
    }

    protected virtual int LayerStoneNoise(int x, int z)
    {
        return GetNoise(x, 0, z, stoneBaseNoise, stoneBaseNoiseHeight, 1);
    }

    protected virtual int LayerDirt(int x, int z)
    {
        int dirtHeight = dirtBaseHeight;
        dirtHeight += GetNoise(x, 100, z, dirtNoise, dirtNoiseHeight, 1);

        return dirtHeight;
    }

    protected virtual void GenerateTerrain(Chunk chunk, int x, int z)
    {
        int stoneHeight = LayerStoneBase(chunk.pos.x + x, chunk.pos.z + z);
        stoneHeight += LayerStoneNoise(chunk.pos.x + x, chunk.pos.z + z);

        int dirtHeight = stoneHeight + LayerDirt(chunk.pos.x + x, chunk.pos.z + z);
        //CreateTreeIfValid(x, z, chunk, dirtHeight);

        for (int y = 0; y < Config.Env.ChunkSize; y++)
        {

            if (y + chunk.pos.y <= stoneHeight)
            {
                SetBlock(chunk, "stone", new BlockPos(x, y, z));
            }
            else if (y + chunk.pos.y < dirtHeight)
            {
                SetBlock(chunk, "dirt", new BlockPos(x, y, z));
            }
            else if (y + chunk.pos.y == dirtHeight)
            {
                SetBlock(chunk, "grass", new BlockPos(x, y, z));
            }
            else if (y + chunk.pos.y == dirtHeight + 1 && GetNoise(x + chunk.pos.x, y + chunk.pos.y, z + chunk.pos.z, 10, 10, 1) > 5)
            {
                Block wildGrass = "wildgrass";
                wildGrass.data2 = (byte)(GetNoise(x + chunk.pos.x, y + chunk.pos.y, z + chunk.pos.z, 1, 155, 1) + 100);

                SetBlock(chunk, wildGrass, new BlockPos(x, y, z));
            }

        }

    }

    public static void SetBlock(Chunk chunk, Block block, BlockPos pos, bool replaceBlocks = false)
    {
        if (Chunk.InRange(pos))
        {
            if (replaceBlocks || chunk.GetBlock(pos).type == Block.Air.type)
            {
                block.modified = false;
                chunk.SetBlock(pos, block, false);
            }
        }
    }

    public int GetNoise(int x, int y, int z, float scale, int max, float power)
    {
        float noise = (noiseGen.Generate(x * scale, y * scale, z * scale) + 1f) * (max / 2f);

        noise = Mathf.Pow(noise, power);

        return Mathf.FloorToInt(noise);
    }

    void CreateTreeIfValid(int x, int z, Chunk chunk)
    {
        if (GetNoise(x + chunk.pos.x, -10000, z + chunk.pos.z, 100, 100, 1) < 10)
        {
            if (GetNoise(x + chunk.pos.x, 10000, z + chunk.pos.z, 100, 100, 1) < 15)
            {
                int terrainHeight = LayerStoneBase(x + chunk.pos.x, z + chunk.pos.z);
                terrainHeight += LayerStoneNoise(x + chunk.pos.x, z + chunk.pos.z);
                terrainHeight += LayerDirt(x + chunk.pos.x, z + chunk.pos.z);

                treeStructure.OldBuild(chunk.world, chunk.pos, new BlockPos(x, terrainHeight - chunk.pos.y, z), this);
            }
        }
    }

}