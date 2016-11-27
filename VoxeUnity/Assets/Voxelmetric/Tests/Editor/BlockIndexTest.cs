using NUnit.Framework;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Utilities;

public class BlockIndexTest
{
    [Test]
    public void ChunkIndexTest()
    {
        for (int y = 0; y <Env.ChunkSize; y++)
            for (int z = 0; z <Env.ChunkSize; z++)
                for (int x = 0; x<Env.ChunkSize; x++)
                {
                    int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);

                    int xx, yy, zz;
                    Helpers.GetChunkIndex3DFrom1D(index, out xx, out yy, out zz);

                    Assert.AreEqual(xx, x);
                    Assert.AreEqual(yy, y);
                    Assert.AreEqual(zz, z);
                }
    }

    [Test]
    public void IndexTest()
    {
        for (int y = 0; y < Env.ChunkSize; y++)
            for (int z = 0; z < Env.ChunkSize; z++)
                for (int x = 0; x < Env.ChunkSize; x++)
                {
                    int index = Helpers.GetIndex1DFrom3D(
                        x+Env.ChunkPadding,
                        y+Env.ChunkPadding,
                        z+Env.ChunkPadding,
                        Env.ChunkSizeWithPadding,
                        Env.ChunkSizeWithPadding
                        );

                    int xx, yy, zz;
                    Helpers.GetIndex3DFrom1D(index, out xx, out yy, out zz, Env.ChunkSizeWithPadding, Env.ChunkSizeWithPadding);
                    xx -= Env.ChunkPadding;
                    yy -= Env.ChunkPadding;
                    zz -= Env.ChunkPadding;

                    Assert.AreEqual(xx, x);
                    Assert.AreEqual(yy, y);
                    Assert.AreEqual(zz, z);
                }
    }
}
