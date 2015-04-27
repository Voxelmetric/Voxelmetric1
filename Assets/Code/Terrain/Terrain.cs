using UnityEngine;
using System.Collections;

public static class Terrain
{
    public static BlockPos GetBlockPos(Vector3 pos)
    {
        BlockPos blockPos = new BlockPos(
            Mathf.RoundToInt(pos.x),
            Mathf.RoundToInt(pos.y),
            Mathf.RoundToInt(pos.z)
            );

        return blockPos;
    }

    public static BlockPos GetBlockPos(RaycastHit hit, bool adjacent = false)
    {
        Vector3 pos = new Vector3(
            MoveWithinBlock(hit.point.x, hit.normal.x, adjacent),
            MoveWithinBlock(hit.point.y, hit.normal.y, adjacent),
            MoveWithinBlock(hit.point.z, hit.normal.z, adjacent)
            );

        return GetBlockPos(pos);
    }

    static float MoveWithinBlock(float pos, float norm, bool adjacent = false)
    {
        if (pos - (int)pos == 0.5f || pos - (int)pos == -0.5f)
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

        return (float)pos;
    }

    public static bool SetBlock(RaycastHit hit, SBlock block, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return false;

        BlockPos pos = GetBlockPos(hit, adjacent);

        chunk.world.SetBlock(pos.x, pos.y, pos.z, block);

        return true;
    }

    public static bool SetBlock(BlockPos pos, World world, SBlock block)
    {
        Chunk chunk = world.GetChunk(pos.x, pos.y, pos.z);
        if (chunk == null)
            return false;

        chunk.world.SetBlock(pos.x, pos.y, pos.z, block);

        return true;
    }

    public static SBlock GetBlock(RaycastHit hit, bool adjacent = false)
    {
        Chunk chunk = hit.collider.GetComponent<Chunk>();
        if (chunk == null)
            return new SBlock(BlockType.air);

        BlockPos pos = GetBlockPos(hit, adjacent);

        return GetBlock(pos, chunk.world);
    }

    public static SBlock GetBlock(BlockPos pos, World world)
    {
        SBlock block = world.GetBlock(pos.x, pos.y, pos.z);

        return block;
    }
}