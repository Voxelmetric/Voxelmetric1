using System;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;

namespace Voxelmetric.Code.Builders
{
    public abstract class MergedFacesMeshBuilder: IMeshBuilder
    {
        protected static readonly int sideSize = Env.ChunkSize;

        private static bool ExpandX(ChunkBlocks blocks, ref bool[] mask, ushort type, int y1, int z1, ref int x2, int y2, int z2)
        {
            // Check the quad formed by YZ axes and try to expand the X asix
            for (int y = y1; y<y2; ++y)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x2, y, z1);
                for (int z = z1; z<z2; ++z, index += Env.ChunkSizeWithPadding)
                {
                    if (mask[index] || blocks.Get(index).Type!=type)
                        return false;
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            for (int y = y1; y<y2; ++y)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x2, y, z1);
                for (int z = z1; z<z2; ++z, index += Env.ChunkSizeWithPadding)
                    mask[index] = true;
            }

            ++x2;
            return true;
        }

        private static bool ExpandY(ChunkBlocks blocks, ref bool[] mask, ushort type, int x1, int z1, int x2, ref int y2, int z2)
        {
            // Check the quad formed by XZ axes and try to expand the Y asix
            for (int z = z1; z<z2; ++z)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x1, y2, z);
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || blocks.Get(index).Type!=type)
                        return false;
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            for (int z = z1; z<z2; ++z)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x1, y2, z);
                for (int x = x1; x<x2; ++x, ++index)
                    mask[index] = true;
            }

            ++y2;
            return true;
        }

        private static bool ExpandZ(ChunkBlocks blocks, ref bool[] mask, ushort type, int x1, int y1, int x2, int y2, ref int z2)
        {
            // Check the quad formed by XY axes and try to expand the Z asix
            for (int y = y1; y<y2; ++y)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x1, y, z2);
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || blocks.Get(index).Type!=type)
                        return false;
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            for (int y = y1; y<y2; ++y)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x1, y, z2);
                for (int x = x1; x<x2; ++x, ++index)
                    mask[index] = true;
            }

            ++z2;
            return true;
        }

        public void Build(Chunk chunk)
        {
            var blocks = chunk.blocks;
            var pools = chunk.pools;

            bool[] mask = pools.BoolArrayPool.PopExact(Env.ChunkSizeWithPaddingPow3);
            Array.Clear(mask, 0, mask.Length);

            // This compression is essentialy RLE. However, instead of working on 1 axis
            // it works in 3 dimensions.
            for (int y = 0; y<Env.ChunkSize; ++y)
            {
                for (int z = 0; z<Env.ChunkSize; ++z)
                {
                    int index = Helpers.GetChunkIndex1DFrom3D(0, y, z);
                    for (int x = 0; x<Env.ChunkSize; ++x, ++index)
                    {
                        // Skip already checked blocks
                        if (mask[index])
                            continue;

                        mask[index] = true;

                        // Skip air data
                        ushort data = blocks.Get(index).Data;
                        ushort type = (ushort)(data&BlockData.TypeMask);
                        if (type==BlockProvider.AirType)
                            continue;

                        int x1 = x, y1 = y, z1 = z, x2 = x+1, y2 = y+1, z2 = z+1;

                        bool expandX = true;
                        bool expandY = true;
                        bool expandZ = true;
                        bool expand;

                        // Try to expand our box in all axes
                        do
                        {
                            expand = false;
                            
                            if (expandY)
                            {
                                expandY = y2<Env.ChunkSize &&
                                          ExpandY(blocks, ref mask, type, x1, z1, x2, ref y2, z2);
                                expand = expandY;
                            }
                            if (expandZ)
                            {
                                expandZ = z2<Env.ChunkSize &&
                                          ExpandZ(blocks, ref mask, type, x1, y1, x2, y2, ref z2);
                                expand = expand|expandZ;
                            }
                            if (expandX)
                            {
                                expandX = x2 < Env.ChunkSize &&
                                          ExpandX(blocks, ref mask, type, y1, z1, ref x2, y2, z2);
                                expand = expand|expandX;
                            }
                        } while (expand);

                        BuildBox(chunk, x1, y1, z1, x2, y2, z2);
                    }
                }
            }

            pools.BoolArrayPool.Push(mask);
        }

        protected abstract void BuildBox(Chunk chunk, int minX, int minY, int minZ, int maxX, int maxY, int maxZ);
    }
}
