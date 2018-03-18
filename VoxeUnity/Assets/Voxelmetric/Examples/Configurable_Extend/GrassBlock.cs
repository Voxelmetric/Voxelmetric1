using Voxelmetric.Code;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

// This class inherits from CubeBlock so that it renders just like any other
// cube block but it replaces the RandomUpdate function with its own.
// Use this class for a block by setting the config's controller to GrassBlock

public class GrassBlock: CubeBlock
{
    private BlockData air;
    private BlockData dirt;
    private BlockData grass;

    public override void OnInit(BlockProvider blockProvider)
    {
        base.OnInit(blockProvider);

        air = BlockProvider.AirBlock;
        Block blk = blockProvider.GetBlock("dirt");
        dirt = new BlockData(blk.Type, blk.Solid);
        blk = blockProvider.GetBlock("grass");
        grass = new BlockData(blk.Type, blk.Solid);
    }

    // On random Update spread grass to any nearby dirt blocks on the surface
    public override void RandomUpdate(Chunk chunk, ref Vector3Int localPos)
    {
        ChunkBlocks blocks = chunk.Blocks;

        // Let's stay inside bounds
        int minX = localPos.x<=0 ? 0 : 1;
        int maxX = localPos.x>=Env.ChunkSize1 ? 0 : 1;
        int minY = localPos.y<=0 ? 0 : 1;
        int maxY = localPos.y>=Env.ChunkSize1 ? 0 : 1;
        int minZ = localPos.z<=0 ? 0 : 1;
        int maxZ = localPos.z>=Env.ChunkSize1 ? 0 : 1;

        for (int y = -minY; y<=maxY; y++)
        {
            for (int z = -minZ; z<=maxZ; z++)
            {
                for (int x = -minX; x<=maxX; x++)
                {
                    // There has to be dirt above our block
                    int grassIndex = Helpers.GetChunkIndex1DFrom3D(localPos.x+x, localPos.y+y, localPos.z+z);
                    if (!blocks.Get(grassIndex).Equals(dirt))
                        continue;

                    // There has to be air above the dirt
                    int airIndex = grassIndex+Env.ChunkSizePow2;
                    if (blocks.Get(airIndex).Equals(air))
                        blocks.Modify(new ModifyOpBlock(grass, grassIndex, true));
                }
            }
        }
    }
}