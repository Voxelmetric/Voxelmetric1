using System;
using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Common.MemoryPooling;
using Voxelmetric.Code.Data_types;

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
        //! A list of modified blocks
        private BlockData[] m_blocks;

        // Temporary structures
        private int m_posLenBytes;
        private byte[] m_positionsBytes;
        private int m_blkLenBytes;
        private byte[] m_blocksBytes;
        

        public Save(Chunk chunk)
        {
            Chunk = chunk;
            IsDifferential = false;
        }

        public void Reset()
        {
            m_hadDifferentialChange = false;
            MarkAsProcessed();
            
            // Reset temporary buffers
            m_posLenBytes = 0;
            m_positionsBytes = null;
            m_blkLenBytes = 0;
            m_blocksBytes = null;

            // Release the memory allocated by temporary buffers
            m_positions = null;
            m_blocks = null;
        }

        public void MarkAsProcessed()
        {
            // Release the memory allocated by temporary buffers
            m_positions = null;
            m_blocks = null;
        }

        public bool IsBinarizeNecessary()
        {
            if (m_blocks==null &&
                !Features.UseDifferentialSerialization_ForceSaveHeaders &&
                !m_hadDifferentialChange)
                return false;

            return true;
        }

        public bool Binarize(BinaryWriter bw)
        {
            // Do not serialize if there's no chunk data and empty chunk serialization is turned off
            if (m_blocks==null)
            {
                if (!Features.UseDifferentialSerialization_ForceSaveHeaders &&
                    !m_hadDifferentialChange
                    )
                    return false;
                m_hadDifferentialChange = false;
            }

            bw.Write(SaveVersion);
            bw.Write((byte)(Features.UseDifferentialSerialization ? 1 : 0));
            bw.Write(Env.ChunkSizePow3);
            bw.Write(Chunk.blocks.NonEmptyBlocks);

            // Chunk data
            if (Features.UseDifferentialSerialization)
            {
                if(m_blocks==null)
                    bw.Write(0);
                else
                {
                    bw.Write(m_blocks.Length);
                    try
                    {
                        bw.Write(m_positionsBytes, 0, m_posLenBytes);
                    }
                    catch (Exception ex)
                    {
                        string s = ex.Message;
                    }
                    bw.Write(m_blocksBytes, 0, m_blkLenBytes);
                }
            }
            else
            {
                // Write compressed data to file
                bw.Write(m_blkLenBytes);
                bw.Write(m_blocksBytes, 0, m_blkLenBytes);
            }

            // We no longer need the temporary buffers
            m_posLenBytes = 0;
            m_positionsBytes = null;
            m_blkLenBytes = 0;
            m_blocksBytes = null;

            return true;
        }

        public bool Debinarize(BinaryReader br)
        {
            bool success = true;

            // Read the version number
            int version = br.ReadInt16();
            if (version!=SaveVersion)
                return false;

            // 0/1 allowed for IsDifferential
            byte isDifferential = br.ReadByte();
            if (isDifferential!=0 && isDifferential!=1)
            {
                success = false;
                goto deserializeFail;
            }
            IsDifferential = isDifferential==1;

            // Current chunk size must match the saved chunk size
            int chunkBlocks = br.ReadInt32();
            if (chunkBlocks!=Env.ChunkSizePow3)
            {
                success = false;
                goto deserializeFail;
            }

            // NonEmptyBlocks must be a sane number in chunkBlocks range
            int nonEmptyBlocks = br.ReadInt32();
            if (nonEmptyBlocks<0 || nonEmptyBlocks>chunkBlocks)
            {
                success = false;
                goto deserializeFail;
            }
            Chunk.blocks.NonEmptyBlocks = nonEmptyBlocks;
            
            while (true)
            {
                if (IsDifferential)
                {
                    int lenBlocks = br.ReadInt32();
                    if (lenBlocks > 0)
                    {
                        int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                        int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                        m_posLenBytes = lenBlocks * blockPosSize;
                        m_blkLenBytes = lenBlocks * blockDataSize;

                        m_positionsBytes = new byte[m_posLenBytes];
                        if (br.Read(m_positionsBytes, 0, m_posLenBytes)!=m_posLenBytes)
                        {
                            // Length must match
                            success = false;
                            goto deserializeFail;
                        }

                        m_blocksBytes = new byte[m_blkLenBytes];
                        if (br.Read(m_blocksBytes, 0, m_blkLenBytes)!=m_blkLenBytes)
                        {
                            // Length must match
                            success = false;
                            goto deserializeFail;
                        }
                    }
                    else
                    {
                        m_blocksBytes = null;
                        m_positionsBytes = null;
                    }
                }
                else
                {
                    // If somebody switched from full to differential serialization, make it so that the next time the chunk is serialized it's saved as diff
                    if (Features.UseDifferentialSerialization)
                        m_hadDifferentialChange = true;
                    
                    m_blkLenBytes = br.ReadInt32();
                    m_blocksBytes = new byte[m_blkLenBytes];

                    // Read raw data
                    int readLength = br.Read(m_blocksBytes, 0, m_blkLenBytes);
                    if (readLength!= m_blkLenBytes)
                    {
                        // Length must match
                        success = false;
                        goto deserializeFail;
                    }
                }

                break;
            }
        deserializeFail:
            if (!success)
            {
                // Revert any changes we performed on our chunk
                Chunk.blocks.NonEmptyBlocks = -1;

                m_posLenBytes = 0;
                m_positionsBytes = null;
                m_blkLenBytes = 0;
                m_blocksBytes = null;
            }

            return success;
        }

        public bool DoCompression()
        {
            if (Features.UseDifferentialSerialization)
            {
                if (m_blocks != null && m_blocks.Length>0)
                {
                    var provider = Chunk.world.blockProvider;
                    int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                    int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                    m_posLenBytes = m_blocks.Length * blockPosSize;
                    m_blkLenBytes = m_blocks.Length * blockDataSize;
                    m_positionsBytes = new byte[m_posLenBytes];
                    m_blocksBytes = new byte[m_blkLenBytes];

                    unsafe
                    {
                        // Pack positions to a byte array
                        fixed (byte* pDst = m_positionsBytes)
                        {
                            for (int i = 0, j = 0; i < m_blocks.Length; i++, j += blockPosSize)
                            {
                                *(BlockPos*)&pDst[j] = m_positions[i];
                            }
                        }
                        // Pack block data to a byte array
                        fixed (BlockData* pBD = m_blocks)
                        fixed (byte* pDst = m_blocksBytes)
                        {
                            for (int i = 0, j = 0; i < m_blocks.Length; i++, j += blockDataSize)
                            {
                                BlockData* bd = &pBD[i];
                                // Convert block types from internal optimized version into global types
                                ushort typeInConfig = provider.GetConfig(bd->Type).typeInConfig;

                                *(BlockData*)&pDst[j] = new BlockData(typeInConfig, bd->Solid);
                            }
                        }
                    }
                }
                else
                {
                    m_posLenBytes = 0;
                    m_blkLenBytes = 0;
                    m_positionsBytes = null;
                    m_blocksBytes = null;
                }
            }
            else
            {
                LocalPools pools = Chunk.pools;
                var provider = Chunk.world.blockProvider;

                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;
                int requestedByteSize = Env.ChunkSizePow3 * blockDataSize;

                // Pop large enough buffers from the pool
                byte[] tmp = pools.ByteArrayPool.Pop(requestedByteSize);
                byte[] bytesCompressed = pools.ByteArrayPool.Pop(requestedByteSize);
                {
                    ChunkBlocks blocks = Chunk.blocks;
                    int i = 0;
                    for (int y = 0; y<Env.ChunkSize; y++)
                    {
                        for (int z = 0; z<Env.ChunkSize; z++)
                        {
                            for (int x = 0; x<Env.ChunkSize; x++, i += blockDataSize)
                            {
                                int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);

                                // Convert block types from internal optimized version into global types
                                BlockData bd = blocks.Get(index);
                                ushort typeInConfig = provider.GetConfig(bd.Type).typeInConfig;

                                // Write updated block data to destination buffer
                                unsafe
                                {
                                    fixed (byte* pDst = tmp)
                                    {
                                        *(BlockData*)&pDst[i] = new BlockData(typeInConfig, bd.Solid);
                                    }
                                }
                            }
                        }
                    }

                    // Compress bytes
                    m_blkLenBytes = CLZF2.lzf_compress(tmp, requestedByteSize, ref bytesCompressed);
                    m_blocksBytes = new byte[m_blkLenBytes];

                    // Copy data from a temporary buffer to block buffer
                    Array.Copy(bytesCompressed, 0, m_blocksBytes, 0, m_blkLenBytes);
                }
                // Return our temporary buffer back to the pool
                pools.ByteArrayPool.Push(bytesCompressed);
                pools.ByteArrayPool.Push(tmp);
            }

            return true;
        }

        public bool DoDecompression()
        {
            LocalPools pools = Chunk.pools;
            var provider = Chunk.world.blockProvider;

            if (IsDifferential)
            {
                if (m_posLenBytes>0 && m_blkLenBytes>0)
                {
                    int blockPosSize = StructSerialization.TSSize<BlockPos>.ValueSize;
                    int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;

                    m_positions = new BlockPos[m_posLenBytes/blockPosSize];
                    m_blocks = new BlockData[m_blkLenBytes/blockDataSize];

                    int i, j;
                    unsafe
                    {
                        // Extract positions
                        fixed (byte* pSrc = m_positionsBytes)
                        {
                            for (i = 0, j = 0; i<m_posLenBytes; i += blockPosSize, j++)
                            {
                                m_positions[j] = *(BlockPos*)&pSrc[i];
                            }
                        }
                        // Extract block data
                        fixed (byte* pSrc = m_blocksBytes)
                        {
                            for (i = 0, j = 0; i<m_blkLenBytes; i += blockDataSize, j++)
                            {
                                BlockData* bd = (BlockData*)&pSrc[i];
                                // Convert global block types into internal optimized version
                                ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                                m_blocks[j] = new BlockData(type, bd->Solid);
                            }
                        }
                    }
                }
            }
            else
            {
                int blockDataSize = StructSerialization.TSSize<BlockData>.ValueSize;
                int requestedByteSize = Env.ChunkSizePow3*blockDataSize;

                // Pop a large enough buffers from the pool
                var bytes = pools.ByteArrayPool.Pop(requestedByteSize);
                {
                    // Decompress data
                    int decompressedLength = CLZF2.lzf_decompress(m_blocksBytes, m_blkLenBytes, ref bytes);
                    if (decompressedLength!=Env.ChunkSizePow3*blockDataSize)
                    {
                        m_blkLenBytes = 0;
                        m_blocksBytes = null;
                        return false;
                    }

                    // Fill chunk with decompressed data
                    ChunkBlocks blocks = Chunk.blocks;
                    int i = 0;
                    unsafe
                    {
                        fixed (byte* pSrc = bytes)
                        {
                            for (int y = 0; y<Env.ChunkSize; y++)
                            {
                                for (int z = 0; z<Env.ChunkSize; z++)
                                {
                                    for (int x = 0; x<Env.ChunkSize; x++, i += blockDataSize)
                                    {
                                        int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                                        BlockData* bd = (BlockData*)&pSrc[i];

                                        // Convert global block type into internal optimized version
                                        ushort type = provider.GetTypeFromTypeInConfig(bd->Type);

                                        blocks.SetRaw(index, new BlockData(type, bd->Solid));
                                    }
                                }
                            }
                        }
                    }
                }
                // Return our temporary buffer back to the pool
                pools.ByteArrayPool.Push(bytes);
            }

            // We no longer need the temporary buffers
            m_posLenBytes = 0;
            m_positionsBytes = null;
            m_blkLenBytes = 0;
            m_blocksBytes = null;

            return true;
        }

        public void ConsumeChanges()
        {
            if (!Features.UseDifferentialSerialization)
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
            if (m_blocks!=null)
            {
                for (int i = 0; i<m_blocks.Length; i++)
                {
                    BlockPos pos = m_positions[i];
                    Chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z), m_blocks[i]);
                }
            }

            MarkAsProcessed();
        }
    }
}