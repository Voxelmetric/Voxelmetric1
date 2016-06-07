using NUnit.Framework;
using UnityEngine;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

public class BlockPosTest {
    [Test]
    public void ContainingChunkCoordinatesTest() {
        AssertContainingChunkCoordinates(new BlockPos(0, 0, 0));
        AssertContainingChunkCoordinates(new BlockPos(-1, -1, -1));
        AssertContainingChunkCoordinates(new BlockPos(-15, -11, -4));
        AssertContainingChunkCoordinates(new BlockPos(-35, -21, -127));
        AssertContainingChunkCoordinates(new BlockPos(15, 11, 4));
        AssertContainingChunkCoordinates(new BlockPos(16, 11, 4));
        AssertContainingChunkCoordinates(new BlockPos(35, 21, 127));
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
