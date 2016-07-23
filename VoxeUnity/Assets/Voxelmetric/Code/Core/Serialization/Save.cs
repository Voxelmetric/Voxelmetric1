using System.Collections.Generic;
using System.IO;
using Voxelmetric.Code.Common.IO;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Core.Serialization
{
    public class Save: IBinarizable
    {
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
                //remove any existing blocks in the dictionary as they're
                //from the existing save and are overwritten
                blocksDictionary.Remove(pos);
                blocksDictionary.Add(pos, chunk.blocks.Get(new Vector3Int(pos.x,pos.y,pos.z)));
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

        public void Binarize(BinaryWriter bw)
        {
            // Convert block types from an internal optimized version into global types
            ushort[] tmp = new ushort[blocks.Length];
            for (int i = 0; i < blocks.Length; i++)
                tmp[i] = Chunk.world.blockProvider.GetConfig(blocks[i].Type).typeInConfig;

            var positionsBytes = StructSerialization.SerializeArray(positions, Chunk.pools.MarshaledPool);
            var blocksBytes = StructSerialization.SerializeArray(tmp, Chunk.pools.MarshaledPool);

            bw.Write(positionsBytes.Length);
            bw.Write(blocksBytes.Length);
            bw.Write(positionsBytes);
            bw.Write(blocksBytes);
        }

        public void Debinarize(BinaryReader br)
        {
            var positionsBytes = new byte[br.ReadInt32()];
            var blockBytes = new byte[br.ReadInt32()];
            positions = StructSerialization.DeserializeArray<BlockPos>(br.ReadBytes(positionsBytes.Length), Chunk.pools.MarshaledPool);
            var tmp = StructSerialization.DeserializeArray<ushort>(br.ReadBytes(blockBytes.Length), Chunk.pools.MarshaledPool);

            // Convert block types global types into internal optimized version
            blocks = new BlockData[tmp.Length];
            for (int i = 0; i < blocks.Length; i++)
                blocks[i] = new BlockData(Chunk.world.blockProvider.GetTypeFromTypeInConfig(tmp[i]));
        }
    }
}