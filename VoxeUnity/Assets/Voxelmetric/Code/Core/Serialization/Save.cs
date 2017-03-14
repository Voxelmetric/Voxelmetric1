using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Serialization
{
    public class Save: IBinarizable
    {
        public static readonly short SaveVersion = 1;

        public BlockPos[] positions;
        public BlockData[] blocks;
        public readonly bool changed = false;

        public Chunk Chunk { get; private set; }

        public Save(Chunk chunk)
        {
            Chunk = chunk;
        }

        public Save(Chunk chunk, Save existing)
        {
            Chunk = chunk;

            Dictionary<BlockPos, BlockData> blocksDictionary = new Dictionary<BlockPos, BlockData>();

            if (existing != null)
            {
                //Because existing saved blocks aren't marked as modified we have to add the
                //blocks already in the save file if there is one.
                existing.AddBlocks(blocksDictionary);
            }

            // Then add modified blocks from this chunk
            for (int i = 0; i<chunk.blocks.modifiedBlocks.Count; i++)
            {
                var pos = chunk.blocks.modifiedBlocks[i];
                // Remove any existing blocks in the dictionary. They come from the existing save and are overwritten
                blocksDictionary.Remove(pos);
                blocksDictionary.Add(pos, chunk.blocks.Get(Helpers.GetChunkIndex1DFrom3D(pos.x,pos.y,pos.z)));
                changed = true;
            }

            blocks = new BlockData[blocksDictionary.Keys.Count];
            positions = new BlockPos[blocksDictionary.Keys.Count];

            int index = 0;
            foreach (var pair in blocksDictionary)
            {
                blocks[index] = pair.Value;
                positions[index] = pair.Key;
                index++;
            }
        }

        private void AddBlocks(Dictionary<BlockPos, BlockData> blocksDictionary)
        {
            for (int i = 0; i < blocks.Length; i++)
            {
                blocksDictionary.Add(positions[i], blocks[i]);
            }
        }

        public bool Binarize(BinaryWriter bw)
        {
            BlockData[] tmp = new BlockData[blocks.Length];

            // Convert block types from an internal optimized version into global types
            for (int i = 0; i<blocks.Length; i++)
            {
                ushort typeInConfig = Chunk.world.blockProvider.GetConfig(blocks[i].Type).typeInConfig;
                tmp[i] = new BlockData(typeInConfig, tmp[i].Solid, tmp[i].Transparent, tmp[i].Rotation);
            }

            var positionsBytes = StructSerialization.SerializeArray(positions, Chunk.pools.MarshaledPool);
            var blocksBytes = StructSerialization.SerializeArray(tmp, Chunk.pools.MarshaledPool);
            
            bw.Write(SaveVersion);

            ChunkBounds bounds = Chunk.m_bounds;
            bw.Write(bounds.minX);
            bw.Write(bounds.minY);
            bw.Write(bounds.minZ);
            bw.Write(bounds.maxX);
            bw.Write(bounds.maxY);
            bw.Write(bounds.maxZ);
            bw.Write(bounds.lowestEmptyBlock);

            bw.Write(positionsBytes.Length);
            bw.Write(blocksBytes.Length);
            bw.Write(positionsBytes);
            bw.Write(blocksBytes);

            return true;
        }

        public bool Debinarize(BinaryReader br)
        {
            // Read the version number
            int version = br.ReadInt16(); // not used for anything at the moment

            ChunkBounds bounds = Chunk.m_bounds;
            bounds.minX = br.ReadInt32();
            bounds.minY = br.ReadInt32();
            bounds.minZ = br.ReadInt32();
            bounds.maxX = br.ReadInt32();
            bounds.maxY = br.ReadInt32();
            bounds.maxZ = br.ReadInt32();
            bounds.lowestEmptyBlock = br.ReadInt32();

            var positionsBytes = new byte[br.ReadInt32()];
            var blockBytes = new byte[br.ReadInt32()];
            positions = StructSerialization.DeserializeArray<BlockPos>(br.ReadBytes(positionsBytes.Length), Chunk.pools.MarshaledPool);
            var tmp = StructSerialization.DeserializeArray<BlockData>(br.ReadBytes(blockBytes.Length), Chunk.pools.MarshaledPool);

            var provider = Chunk.world.blockProvider;

            // Convert block types global types into internal optimized version
            blocks = new BlockData[tmp.Length];
            for (int i = 0; i<blocks.Length; i++)
            {
                ushort type = provider.GetTypeFromTypeInConfig(tmp[i].Type);
                blocks[i] = new BlockData(type, tmp[i].Solid, tmp[i].Transparent, tmp[i].Rotation);
            }

            // Consume info about invalidated chunk
            Chunk.blocks.recalculateBounds = false;
            return true;
        }
    }
}