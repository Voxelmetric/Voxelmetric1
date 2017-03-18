using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
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
        public BlockPos[] Positions { get; private set; }
        //! A list if modified blocks
        public BlockData[] Blocks { get; private set; }

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
            Positions = null;
            Blocks = null;
        }

        public bool IsBinarizeNecessary()
        {
            if (Blocks==null && Utilities.Core.UseDifferentialSerialization &&
                !Utilities.Core.UseDifferentialSerialization_ForceSaveHeaders &&
                !m_hadDifferentialChange)
                return false;

            return true;
        }

        public bool Binarize(BinaryWriter bw)
        {
            // Do not serialize if there's no chunk data and empty chunk serialization is turned off
            if (Blocks==null)
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
                if (Blocks!=null)
                {
                    BlockData[] tmp = new BlockData[Blocks.Length];
                    var provider = Chunk.world.blockProvider;

                    // Convert block types from internal optimized version into global types
                    for (int i = 0; i < Blocks.Length; i++)
                    {
                        BlockData bd = Blocks[i];
                        ushort typeInConfig = provider.GetConfig(bd.Type).typeInConfig;
                        tmp[i] = new BlockData(typeInConfig, bd.Solid, bd.Rotation);
                    }

                    var positionsBytes = StructSerialization.SerializeArray(Chunk.pools.MarshaledPool, Positions);
                    var blocksBytes = StructSerialization.SerializeArray(Chunk.pools.MarshaledPool, tmp);

                    bw.Write(positionsBytes.Length);
                    bw.Write(blocksBytes.Length);
                    bw.Write(positionsBytes);
                    bw.Write(blocksBytes);
                }
                else
                {
                    bw.Write(0);
                    bw.Write(0);
                }
            }
            else
            {
                BlockData[] tmp = new BlockData[Env.ChunkSizePow3];
                var provider = Chunk.world.blockProvider;

                // Convert block types from internal optimized version into global types
                ChunkBlocks blocks = Chunk.blocks;
                int i = 0;
                for (int y = 0; y<Env.ChunkSize; y++)
                {
                    for (int z = 0; z<Env.ChunkSize; z++)
                    {
                        for (int x = 0; x<Env.ChunkSize; x++, i++)
                        {
                            int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                            
                            BlockData bd = blocks.Get(index);
                            ushort typeInConfig = provider.GetConfig(bd.Type).typeInConfig;
                            tmp[i] = new BlockData(typeInConfig, bd.Solid, bd.Rotation);
                        }
                    }
                }

                // Retrieve a long enough buffer from the pool
                var bytes = Chunk.pools.ByteArrayPool.Pop(Env.ChunkSizePow3*2);

                // Convert BlockData to pure bytes
                var blocksBytes = StructSerialization.SerializeArray(Chunk.pools.MarshaledPool, tmp);
                // Compress bytes
                int compressedLength = CLZF2.lzf_compress(blocksBytes, -1, ref bytes);

                // Write compressed data to file
                bw.Write(compressedLength);
                bw.Write(bytes);

                // Return temporary buffer back to pool
                Chunk.pools.ByteArrayPool.Push(bytes);
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
                var positionsBytes = new byte[br.ReadInt32()];
                var blockBytes = new byte[br.ReadInt32()];
                Positions = StructSerialization.DeserializeArray<BlockPos>(Chunk.pools.MarshaledPool, br.ReadBytes(positionsBytes.Length));
                var tmp = StructSerialization.DeserializeArray<BlockData>(Chunk.pools.MarshaledPool, br.ReadBytes(blockBytes.Length));

                var provider = Chunk.world.blockProvider;

                // Convert block types global types into internal optimized version. Let's keep them in temporary arrays
                Blocks = new BlockData[tmp.Length];
                for (int i = 0; i<Blocks.Length; i++)
                {
                    ushort type = provider.GetTypeFromTypeInConfig(tmp[i].Type);
                    Blocks[i] = new BlockData(type, tmp[i].Solid, tmp[i].Rotation);
                }
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

                    // Transform compressed data into an array of BlockData
                    var tmp = StructSerialization.DeserializeArray<BlockData>(Chunk.pools.MarshaledPool, bytes, decompressedLength);
                    var provider = Chunk.world.blockProvider;

                    // Copy decompressed blocks directly into chunk
                    ChunkBlocks blocks = Chunk.blocks;
                    int i = 0;
                    for (int y = 0; y<Env.ChunkSize; y++)
                    {
                        for (int z = 0; z<Env.ChunkSize; z++)
                        {
                            for (int x = 0; x<Env.ChunkSize; x++, i++)
                            {
                                // Convert global block type into internal optimized version
                                ushort type = provider.GetTypeFromTypeInConfig(tmp[i].Type);

                                int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                                blocks.SetRaw(index, new BlockData(type, tmp[i].Solid, tmp[i].Rotation));
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

            Blocks = new BlockData[blocksDictionary.Keys.Count];
            Positions = new BlockPos[blocksDictionary.Keys.Count];

            int index = 0;
            foreach (var pair in blocksDictionary)
            {
                Blocks[index] = pair.Value;
                Positions[index] = pair.Key;
                index++;
            }
        }

        public void CommitChanges()
        {
            if (!IsDifferential)
                return;

            // Rewrite generated blocks with differential positions
            for (int i = 0; i < Blocks.Length; i++)
            {
                BlockPos pos = Positions[i];
                Chunk.blocks.SetRaw(Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z), Blocks[i]);
            }

            MaskAsProcessed();
        }
    }
}