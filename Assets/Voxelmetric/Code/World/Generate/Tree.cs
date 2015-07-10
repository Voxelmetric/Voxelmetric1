using UnityEngine;
using SimplexNoise;

public class StructureTree {
    public static void Build(Chunk chunk, BlockPos pos, TerrainGen terrainGen)
    {
        int leaves = terrainGen.GetNoise(pos.x + chunk.pos.x, 0, pos.z + chunk.pos.z, 1f, 2, 1) +1;

        for (int x = -leaves; x <= leaves; x++)
        {
            for (int y = 3; y <= 6; y++)
            {
                for (int z = -leaves; z <= leaves; z++)
                {
                    TerrainGen.SetBlock(chunk, "leaves", pos.Add(x,y,z), true);
                }
            }
        }
        for (int y = 0; y <= 5; y++)
        {
            TerrainGen.SetBlock(chunk, "log", pos.Add(0, y, 0), true);
        }
    }


    public static bool ChunkContains(Chunk chunk, BlockPos pos)
    {
        //          fpy
        //           | fpz
        //           | /
        //           |/
        //   fnx-----x-------fpx
        //          /|
        //         / |
        //       fnz |
        //          fny


        int fpy = pos.y+6;
        int fny = pos.y-0;
        int fpx = pos.x+3;
        int fnx = pos.x-3;
        int fpz = pos.z+3;
        int fnz = pos.z-3;

        if (fpy < chunk.pos.y)
            return false;

        if (fny > (chunk.pos.y + Config.Env.ChunkSize))
            return false;

        if (fpx < chunk.pos.x)
            return false;

        if (fnx > (chunk.pos.x + Config.Env.ChunkSize))
            return false;

        if (fpz < chunk.pos.z)
            return false;

        if (fnz > (chunk.pos.z + Config.Env.ChunkSize))
            return false;

        return true;
    }
}
