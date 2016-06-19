using NUnit.Framework;
using UnityEngine;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

public class BlockPosTest {
    [Test]
    public void ContainingChunkCoordinatesTest() {
        for(int x=-Env.ChunkSize; x<=2*Env.ChunkSize; x++)
            for (int y = -Env.ChunkSize; y <= 2 * Env.ChunkSize; y++)
                for (int z = -Env.ChunkSize; z <= 2 * Env.ChunkSize; z++)
                    AssertContainingChunkCoordinates(new BlockPos(x, y, z));
    }

    private static void AssertContainingChunkCoordinates(BlockPos pos) {
        Assert.AreEqual(ExpContainingChunkCoordinates(pos), pos.ContainingChunkCoordinates(), pos.ToString());
    }

    //returns the position of the chunk containing this block
    private static BlockPos ExpContainingChunkCoordinates(BlockPos pos) {
        int chunkSize = Env.ChunkSize;

        int cx = Mathf.FloorToInt(pos.x / (float)chunkSize) * chunkSize;
        int cy = Mathf.FloorToInt(pos.y / (float)chunkSize) * chunkSize;
        int cz = Mathf.FloorToInt(pos.z / (float)chunkSize) * chunkSize;

        return new BlockPos(cx, cy, cz);
    }
}
