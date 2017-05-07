using System;
using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities.Noise;

public class StructureTree: GeneratedStructure
{
    private WorldBlocks blocks;
    private BlockData leaves;
    private BlockData log;

    private static readonly int minCrownSize = 3;
    private static readonly int maxTrunkSize = 3;

    public override void Init(World world)
    {
        blocks = world.blocks;
        Block blk = world.blockProvider.GetBlock("leaves");
        leaves = new BlockData(blk.Type, blk.Solid);
        blk = world.blockProvider.GetBlock("log");
        log = new BlockData(blk.Type, blk.Solid);
    }

    public override void Build(World world, ref Vector3Int worldPos, TerrainLayer layer)
    {
        int noise = Helpers.FastFloor(NoiseUtils.GetNoise(layer.Noise.Noise, worldPos.x, worldPos.y, worldPos.z, 1f, minCrownSize, 1f));
        int leavesRange = noise + 3;
        int leavesRange1 = leavesRange-1;
        int trunkHeight = maxTrunkSize - noise;

        // Make the crown an ellipsoid flattened on the y axis
        float a2inv = 1.0f/(leavesRange*leavesRange);
        float b2inv = 1.0f/(leavesRange1*leavesRange1);
        
        int x1 = worldPos.x-leavesRange;
        int x2 = worldPos.x+leavesRange;
        int y1 = worldPos.y+1+trunkHeight;
        int y2 = y1 + 1+2*leavesRange1;
        int z1 = worldPos.z-leavesRange;
        int z2 = worldPos.z+leavesRange;
        
        // Generate the crown
        Vector3Int posFrom = new Vector3Int(x1, y1, z1);
        Vector3Int posTo = new Vector3Int(x2, y2, z2);
        Vector3Int chunkPosFrom = Chunk.ContainingChunkPos(ref posFrom);
        Vector3Int chunkPosTo = Chunk.ContainingChunkPos(ref posTo);
        
        int minY = Helpers.Mod(posFrom.y, Env.ChunkSize);
        for (int cy = chunkPosFrom.y; cy <= chunkPosTo.y; cy += Env.ChunkSize, minY = 0)
        {
            int maxY = Math.Min(posTo.y - cy, Env.ChunkSize1);
            int minZ = Helpers.Mod(posFrom.z, Env.ChunkSize);
            for (int cz = chunkPosFrom.z; cz <= chunkPosTo.z; cz += Env.ChunkSize, minZ = 0)
            {
                int maxZ = Math.Min(posTo.z - cz, Env.ChunkSize1);
                int minX = Helpers.Mod(posFrom.x, Env.ChunkSize);
                for (int cx = chunkPosFrom.x; cx <= chunkPosTo.x; cx += Env.ChunkSize, minX = 0)
                {
                    Vector3Int chunkPos = new Vector3Int(cx, cy, cz);
                    Chunk ch = world.chunks.Get(ref chunkPos);
                    if (ch == null)
                        continue;

                    int maxX = Math.Min(posTo.x-cx, Env.ChunkSize1);
                    for (int y = minY; y <= maxY; ++y)
                    {
                        for (int z =minZ; z <= maxZ; ++z)
                        {
                            for (int x = minX; x <= maxX; ++x)
                            {
                                float xx = cx+x-worldPos.x;
                                float yy = cy+y-y1-leavesRange1;
                                float zz = cz+z-worldPos.z;

                                float _x = xx*xx*a2inv;
                                float _y = yy*yy*b2inv;
                                float _z = zz*zz*a2inv;
                                if (_x+_y+_z<=1.0f)
                                {
                                    int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                                    ch.blocks.SetRaw(index, leaves);
                                }
                            }
                        }
                    }
                }
            }
        }

        // Genrate the trunk
        blocks.SetRaw(ref worldPos, log);
        for (int y = 1; y <= trunkHeight; y++)
        {
            Vector3Int blockPos = worldPos.Add(0, y, 0);
            blocks.SetRaw(ref blockPos, log);
        }
    }
}