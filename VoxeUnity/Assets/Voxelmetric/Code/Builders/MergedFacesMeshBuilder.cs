using System;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Builders
{
    public abstract class MergedFacesMeshBuilder: AMeshBuilder
    {
        protected readonly int m_sideSize;
        protected readonly int m_pow;
        protected readonly float m_scale;

        protected MergedFacesMeshBuilder(float scale, int sideSize)
        {
            m_scale = scale;
            m_pow = 1+(int)Math.Log(sideSize, 2);
            m_sideSize = sideSize;
        }

        private bool ExpandX(ChunkBlocks blocks, ref bool[] mask, Block block, int y1, int z1, ref int x2, int y2, int z2)
        {
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int yOffset = sizeWithPaddingPow2 - (z2-z1)* sizeWithPadding;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x2, y1, z1, m_pow);

            // Check the quad formed by YZ axes and try to expand the X axis
            int index = index0;
            for (int y = y1; y<y2; ++y, index+=yOffset)
            {
                for (int z = z1; z<z2; ++z, index += sizeWithPadding)
                {
                    if (mask[index] || !CanCreateBox(block, blocks.GetBlock(index)))
                        return false;
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            index = index0;
            for (int y = y1; y<y2; ++y, index+=yOffset)
            {
                for (int z = z1; z<z2; ++z, index += sizeWithPadding)
                    mask[index] = true;
            }

            ++x2;
            return true;
        }

        private bool ExpandY(ChunkBlocks blocks, ref bool[] mask, Block block, int x1, int z1, int x2, ref int y2, int z2)
        {
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;

            int zOffset = sizeWithPadding - x2+x1;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x1, y2, z1, m_pow);

            // Check the quad formed by XZ axes and try to expand the Y axis
            int index = index0;
            for (int z = z1; z<z2; ++z, index+=zOffset)
            {
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || !CanCreateBox(block, blocks.GetBlock(index)))
                        return false;
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            index = index0;
            for (int z = z1; z<z2; ++z, index+=zOffset)
            {
                for (int x = x1; x<x2; ++x, ++index)
                    mask[index] = true;
            }

            ++y2;
            return true;
        }

        private bool ExpandZ(ChunkBlocks blocks, ref bool[] mask, Block block, int x1, int y1, int x2, int y2, ref int z2)
        {
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int yOffset = sizeWithPaddingPow2 - x2+x1;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x1, y1, z2, m_pow);
            
            // Check the quad formed by XY axes and try to expand the Z axis
            int index = index0;
            for (int y = y1; y<y2; ++y, index+=yOffset)
            {
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || !CanCreateBox(block, blocks.GetBlock(index)))
                        return false;
                }
            }

            // If the box can expand, mark the position as tested and expand the X axis
            index = index0;
            for (int y = y1; y<y2; ++y, index+=yOffset)
            {
                for (int x = x1; x<x2; ++x, ++index)
                    mask[index] = true;
            }

            ++z2;
            return true;
        }

        public override void Build(Chunk chunk)
        {
            var blocks = chunk.blocks;
            var pools = chunk.pools;

            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int sizeWithPaddingPow3 = sizeWithPaddingPow2 * sizeWithPadding;

            bool[] mask = pools.BoolArrayPool.PopExact(sizeWithPaddingPow3);
            Array.Clear(mask, 0, mask.Length);

            // This compression is essentialy RLE. However, instead of working on 1 axis
            // it works in 3 dimensions.
            int index = Env.ChunkPadding + (Env.ChunkPadding << m_pow) + (Env.ChunkPadding << (m_pow << 1));
            int yOffset = sizeWithPaddingPow2 - m_sideSize*sizeWithPadding;
            int zOffset = sizeWithPadding-m_sideSize;
            for (int y = 0; y<m_sideSize; ++y, index+=yOffset)
            {
                for (int z = 0; z<m_sideSize; ++z, index+=zOffset)
                {
                    for (int x = 0; x<m_sideSize; ++x, ++index)
                    {
                        // Skip already checked blocks
                        if (mask[index])
                            continue;

                        mask[index] = true;
                        
                        Block block = blocks.GetBlock(index);

                        // Skip blocks we're not interested in right away
                        if (!CanConsiderBlock(block))
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
                                expandY = y2<m_sideSize &&
                                          ExpandY(blocks, ref mask, block, x1, z1, x2, ref y2, z2);
                                expand = expandY;
                            }
                            if (expandZ)
                            {
                                expandZ = z2<m_sideSize &&
                                          ExpandZ(blocks, ref mask, block, x1, y1, x2, y2, ref z2);
                                expand = expand|expandZ;
                            }
                            if (expandX)
                            {
                                expandX = x2<m_sideSize &&
                                          ExpandX(blocks, ref mask, block, y1, z1, ref x2, y2, z2);
                                expand = expand|expandX;
                            }
                        } while (expand);

                        BuildBox(chunk, block, x1, y1, z1, x2, y2, z2);
                    }
                }
            }

            pools.BoolArrayPool.Push(mask);
        }

        protected abstract bool CanConsiderBlock(Block block);
        protected abstract bool CanCreateBox(Block block, Block neighbor);
        protected abstract void BuildBox(Chunk chunk, Block block, int minX, int minY, int minZ, int maxX, int maxY, int maxZ);
    }
}
