using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Core.Serialization
{
    public class Save: IBinarizable
    {
        public static readonly short SaveVersion = 1;

        //! If true and a differential serialization is enabled, it says that there was once a difference
        //! Without this headers would not be serialized if a change was made on chunk that would return it to its default state
        private bool m_hadDifferentialChange = false;
        
        public Chunk Chunk { get; private set; }
        public bool IsDifferential { get; private set; }

        //! A list of modified positions
        private BlockPos[] m_positions;
        //! A list if modified blocks
        private BlockData[] m_blocks;

        public Save(Chunk chunk)
        {
            Chunk = chunk;
            IsDifferential = false;
        }

        public void Reset()
        {
            m_hadDifferentialChange = false;
            MaskAsProcessed();
        }

        public void MaskAsProcessed()
        {
            IsDifferential = false;

            // Release the memory allocated by temporary buffers
            m_positions = null;
            m_blocks = null;
        }

        public bool IsBinarizeNecessary()
        {
            if (m_blocks==null && Utilities.Core.UseDifferentialSerialization &&
                !Utilities.Core.UseDifferentialSerialization_ForceSaveHeaders &&
                !m_hadDifferentialChange)
                return false;

            return true;
        }

        public bool Binarize(BinaryWriter bw)
        {
            // Do not serialize if there's no chunk data and empty chunk serialization is turned off
            if (m_blocks==null)
            {
                if (Utilities.Core.UseDifferentialSerialization &&
                    !Utilities.Core.UseDifferentialSerialization_ForceSaveHeaders &&
                    !m_hadDifferentialChange
                    )
                    return false;
                m_hadDifferentialChange = false;
            }

            bw.Write(SaveVersion);
            bw.Write(Utilities.Core.UseDifferentialSerialization);

            // Chunk bounds
            ChunkBounds bounds = Chunk.m_bounds;
            bw.Write((byte)bounds.minX);
            bw.Write((byte)bounds.minY);
            bw.Write((byte)bounds.minZ);
            bw.Write((byte)bounds.maxX);
            bw.Write((byte)bounds.maxY);
            bw.Write((byte)bounds.maxZ);
            bw.Write((byte)bounds.lowestEmptyBlock);
            bw.Write(Chunk.blocks.NonEmptyBlocks);

            // Chunk data
            if (Utilities.Core.UseDifferentialSerialization)
            {
                if (m_blocks!=null)
                {
                    LocalPools pools = Chunk.pools;
                    var provider = Chunk.world.blockProvider;

                    // Pop large enough buffers from the pool
                    int requestedByteSize = Env.ChunkSizePow3 * StructSerialization.TSSize<BlockData>.ValueSize;
                    byte[] blocksBytes = pools.ByteArrayPool.Pop(requestedByteSize);
                    byte[] positionsBytes = pools.ByteArrayPool.Pop(requestedByteSize);
                    {
                        unsafe
                        {
                            // Pack positions to a byte array
                            fixed (byte* pDst = positionsBytes)
                            {
                                for (int i = 0, j = 0;
                                     i<m_blocks.Length;
                                     i++, j += StructSerialization.TSSize<BlockPos>.ValueSize)
                                {
                                    *(BlockPos*)&pDst[j] = m_positions[i];
                                }
                            }
                            // Pack block data to a byte array
                            fixed (BlockData* pBD = m_blocks)
                            fixed (byte* pDst = blocksBytes)
                            {
                                for (int i = 0, j = 0;
                                     i<m_blocks.Length;
                                     i++, j += StructSerialization.TSSize<BlockData>.ValueSize)
                                {
                                    BlockData* bd = &pBD[i];
                                    // Convert block types from internal optimized version into global types
                                    ushort typeInConfig = provider.GetConfig(bd->Type).typeInConfig;

                                    *(BlockData*)&pDst[j] = new BlockData(typeInConfig, bd->Solid, bd->Rotation);
                                }
                            }
                        }

                        bw.Write(m_blocks.Length);
                        bw.Write(positionsBytes, 0, m_blocks.Length*StructSerialization.TSSize<BlockPos>.ValueSize);
                        bw.Write(blocksBytes, 0, m_blocks.Length*StructSerialization.TSSize<BlockData>.ValueSize);
                    }
                    // Return temporary buffers back to pool
                    pools.ByteArrayPool.Push(positionsBytes);
                    pools.ByteArrayPool.Push(blocksBytes);
                }
                else
                {
                    bw.Write(0);
                    bw.Write(0);
                }
            }
            else
            {
                LocalPools pools = Chunk.pools;
                var provider = Chunk.world.blockProvider;
                
                // Pop large enough buffers from the pool
                int requestedByteSize = Env.ChunkSizePow3*StructSerialization.TSSize<BlockData>.ValueSize;
                byte[] blocksBytes = pools.ByteArrayPool.Pop(requestedByteSize);
                byte[] bytesCompressed = pools.ByteArrayPool.Pop(requestedByteSize);
                {
                    ChunkBlocks blocks = Chunk.blocks;
                    int i = 0;
                    for (int y = 0; y<Env.ChunkSize; y++)
                    {
                        for (int z = 0; z<Env.ChunkSize; z++)
                        {
                            for (int x = 0; x<Env.ChunkSize; x++, i+= StructSerialization.TSSize<BlockData>.ValueSize)
                            {
                                int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);

                                // Convert block types from internal optimized version into global types
                                BlockData bd = blocks.Get(index);
                                ushort typeInConfig = provider.GetConfig(bd.Type).typeInConfig;

                                // Write updated block data to destination buffer
                                unsafe
                                {
                                    fixed (byte* pDst = blocksBytes)
                                    {
                                        *(BlockData*)&pDst[i] = new BlockData(typeInConfig, bd.Solid, bd.Rotation);
                                    }
                                }
                            }
                        }
                    }

                    // Compress bytes
                    int compressedLength = CLZF2.lzf_compress(blocksBytes, requestedByteSize, ref bytesCompressed);

                    // Write compressed data to file
                    bw.Write(compressedLength);
                    bw.Write(bytesCompressed);
                }
                // Return temporary buffer back to pool
                pools.ByteArrayPool.Push(bytesCompressed);
                pools.ByteArrayPool.Push(blocksBytes);
            }

            return true;
        }

        public bool Debinarize(BinaryReader br)
        {
            bool success = true;

            // Read the version number
            int version = br.ReadInt16();
            if (version!=SaveVersion)
                return false;

            IsDifferential = br.ReadBoolean();

            ChunkBounds bounds = Chunk.m_bounds;
            bounds.minX = br.ReadByte();
            bounds.minY = br.ReadByte();
            bounds.minZ = br.ReadByte();
            bounds.maxX = br.ReadByte();
            bounds.maxY = br.ReadByte();
            bounds.maxZ = br.ReadByte();
            bounds.lowestEmptyBlock = br.ReadByte();
            Chunk.blocks.NonEmptyBlocks = br.ReadInt32();

            if (IsDifferential)
            {
                LocalPools pools = Chunk.pools;
                var provider = Chunk.world.blockProvider;

                // Pop large enough buffers from the pool
                int requestedByteSize = Env.ChunkSizePow3 * StructSerialization.TSSize<BlockData>.ValueSize;
                byte[] blocksBytes = pools.ByteArrayPool.Pop(requestedByteSize);
                byte[] positionsBytes = pools.ByteArrayPool.Pop(requestedByteSize);
                {
                    int lenBlocks = br.ReadInt32();
                    int posLenBytes = lenBlocks*StructSerialization.TSSize<BlockPos>.ValueSize;
                    int blkLenBytes = lenBlocks*StructSerialization.TSSize<BlockData>.ValueSize;
                    br.Read(positionsBytes, 0, posLenBytes);
                    br.Read(blocksBytes, 0, blkLenBytes);

                    m_positions = new BlockPos[lenBlocks];
                    m_blocks = new BlockData[lenBlocks];

                    int i, j;
                    unsafe
                    {
                        // Extract positions
                        fixed (byte* pSrc = positionsBytes)
                        {
                            for (i = 0, j = 0; i < posLenBytes; i += StructSerialization.TSSize<BlockPos>.ValueSize, j++)
                            {
                                m_positions[j] = *(BlockPos*)&pSrc[i];
                            }
                        }
                        // Extract block data
                        fixed (byte* pSrc = blocksBytes)
                        {
                            for (i = 0, j = 0; i < blkLenBytes; i += StructSerialization.TSSize<BlockData>.ValueSize, j++)
                            {
                                BlockData *bd = (BlockData*)&pSrc[i];
                                // Convert global block types into internal optimized version
                                ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                                m_blocks[j] = new BlockData(type, bd->Solid, bd->Rotation);
                            }
                        }
                    }
                }
                // Return temporary buffers back to pool
                pools.ByteArrayPool.Push(positionsBytes);
                pools.ByteArrayPool.Push(blocksBytes);
            }
            else
            {
                // If somebody switched from full to differential serialization, make it so that the next time the chunk is serialized it's saved as diff
                if (Utilities.Core.UseDifferentialSerialization)
                    m_hadDifferentialChange = true;

                // Retrieve a long enough buffer from the pool
                var bytesCompressed = Chunk.pools.ByteArrayPool.Pop(Env.ChunkSizePow3 * 2);
                var bytes = Chunk.pools.ByteArrayPool.Pop(Env.ChunkSizePow3 * 2);

                while (true)
                {
                    // Read raw data
                    int compressedLength = br.ReadInt32();
                    int readLength = br.Read(bytesCompressed, 0, compressedLength);
                    if (readLength!=compressedLength)
                    {
                        // Length must match
                        success = false;
                        break;
                    }
                    
                    // Decompress data
                    int decompressedLength = CLZF2.lzf_decompress(bytesCompressed, compressedLength, ref bytes);
                    if (decompressedLength!=Env.ChunkSizePow3*StructSerialization.TSSize<BlockData>.ValueSize)
                    {
                        // Size of decompressed chunk can't be different then ChunkSizePow3
                        success = false;
                        break;
                    }
                    
                    // Fill chunk with decompressed data
                    ChunkBlocks blocks = Chunk.blocks;
                    var provider = Chunk.world.blockProvider;
                    int i=0;
                    unsafe
                    {
                        fixed (byte* pSrc = bytes)
                        {
                            for (int y = 0; y<Env.ChunkSize; y++)
                            {
                                for (int z = 0; z<Env.ChunkSize; z++)
                                {
                                    for (int x = 0; x<Env.ChunkSize; x++, i += StructSerialization.TSSize<BlockData>.ValueSize)
                                    {
                                        int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                                        BlockData* bd = (BlockData*)&pSrc[i];

                                        // Convert global block type into internal optimized version
                                        ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                                        blocks.SetRaw(index, new BlockData(type, bd->Solid, bd->Rotation));
                                    }
                                }
                            }
                        }
                    }

                    break;
                }

                // Return temporary buffer back to pool
                Chunk.pools.ByteArrayPool.Push(bytes);
                Chunk.pools.ByteArrayPool.Push(bytesCompressed);
            }

            return success;
        }

        public void ConsumeChanges()
        {
            if (!Utilities.Core.UseDifferentialSerialization)
                return;

            ChunkBlocks blocks = Chunk.blocks;
            if (blocks.modifiedBlocks.Count<=0)
                return;

            m_hadDifferentialChange = true;

            Dictionary<BlockPos, BlockData> blocksDictionary = new Dictionary<BlockPos, BlockData>();
            
            // Create a map of modified blocks and their positions
            // TODO: Depending on the amount of changes this could become a performance bottleneck
            for (int i = 0; i < blocks.modifiedBlocks.Count; i++)
            {
                var pos = blocks.modifiedBlocks[i];
                // Remove any existing blocks in the dictionary. They come from the existing save and are overwritten
                blocksDictionary.Remove(pos);
                blocksDictionary.Add(pos, blocks.Get(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z)));
            }

            m_blocks = new BlockData[blocksDictionary.Keys.Count];
            m_positions = new BlockPos[blocksDictionary.Keys.Count];

            int index = 0;
            foreach (var pair in blocksDictionary)
            {
                m_blocks[index] = pair.Value;
                m_positions[index] = pair.Key;
                index++;
            }
        }

        public void CommitChanges()
        {
            if (!IsDifferential)
                return;

            // Rewrite generated blocks with differential positions
            for (int i = 0; i < m_blocks.Length; i++)
            {
                BlockPos pos = m_positions[i];
                Chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z), m_blocks[i]);
            }

            MaskAsProcessed();
        }
    }
}