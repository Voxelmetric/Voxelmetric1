using UnityEngine;

public static class Voxelmetric
{

    public static BlockPos GetBlockPos(RaycastHit hit, bool adjacent = false)
    {
        Vector3 pos = new Vector3(
            MoveWithinBlock(hit.point.x, hit.normal.x, adjacent),
            MoveWithinBlock(hit.point.y, hit.normal.y, adjacent),
            MoveWithinBlock(hit.point.z, hit.normal.z, adjacent)
            );

        return pos;
    }

    static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        //Because of float imprecision we can't guarantee a hit on the side of a
        //block will be exactly 0.5 so we add a bit of padding
        float offset = pos - (int)pos;
        if ((offset > 0.49f && offset < 0.51) || (offset > -0.51f && offset < -0.49))
        {
            if (adjacent)
            {
                pos += (norm / 2);
            }
            else
            {
                pos -= (norm / 2);
            }
        }

        return pos;
    }

    public static bool SetBlock(RaycastHit hit, Block block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();

        if (chunk == null)
            return false;

        BlockPos pos = GetBlockPos(hit, adjacent);
        chunk.world.SetBlock(pos, block);

        if (Config.Toggle.BlockLighting)
        {
            BlockLight.LightArea(chunk.world, pos);
        }

        return true;
    }

    public static bool SetBlock(BlockPos pos, World world, Block block)
    {
        Chunk chunk = world.GetChunk(pos);
        if (chunk == null)
            return false;

        chunk.world.SetBlock(pos, block);

        if (Config.Toggle.BlockLighting)
        {
            BlockLight.LightArea(world, pos);
        }

        return true;
    }

    public static Block GetBlock(RaycastHit hit, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return Block.Air;

        BlockPos pos = GetBlockPos(hit, adjacent);

        return GetBlock(pos, chunk.world);
    }

    public static Block GetBlock(BlockPos pos, World world)
    {
        Block block = world.GetBlock(pos);

        return block;
    }
}