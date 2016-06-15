using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.Utilities;
using Voxelmetric.Code.VM;

namespace Voxelmetric.Code.Core
{
    public sealed class ChunkBlocks
    {
        public Chunk chunk { get; private set; }
        private readonly BlockData[] blocks = Helpers.CreateArray1D<BlockData>(Env.ChunkVolume);
        
        //! Queue of setBlock operations to execute
        private readonly List<SetBlockContext> m_setBlockQueue = new List<SetBlockContext>();

        private byte[] receiveBuffer;
        private int receiveIndex;

        public readonly List<BlockPos> modifiedBlocks = new List<BlockPos>();
        public bool contentsModified;

        private static byte[] emptyBytes;
        public static byte[] EmptyBytes
        {
            get
            {
                if (emptyBytes==null)
                    emptyBytes = new byte[16384]; // TODO: Validate whether this is fine
                return emptyBytes;
            }
        }

        //! Number of blocks which are not air (non-empty blocks)
        public int NonEmptyBlocks { get; private set; }

        public ChunkBlocks(Chunk chunk)
        {
            this.chunk = chunk;
        }

        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("contentsModified=");
            sb.Append(contentsModified.ToString());
            return sb.ToString();
        }

        public void Reset()
        {
            Array.Clear(blocks, 0, blocks.Length);

            contentsModified = false;
            modifiedBlocks.Clear();

            NonEmptyBlocks = 0;
        }

        public void Update()
        {
            if (m_setBlockQueue.Count<=0)
                return;

            chunk.RequestBuildVertices();

            int rebuildMask = 0;

            // Modify blocks
            for (int j = 0; j < m_setBlockQueue.Count; j++)
            {
                SetBlockContext context = m_setBlockQueue[j];

                // Update non-empty block count
                if (context.Block.Type == BlockProvider.AirType)
                    --NonEmptyBlocks;
                else
                    ++NonEmptyBlocks;

                int x, y, z;
                Helpers.GetChunkIndex3DFrom1D(context.Index, out x, out y, out z);

                BlockPos pos = new BlockPos(x, y, z);
                BlockPos globalPos = pos + chunk.pos;

                BlockData oldBlockData = blocks[context.Index];

                Block oldBlock = chunk.world.blockProvider.BlockTypes[oldBlockData.Type];
                Block newBlock = chunk.world.blockProvider.BlockTypes[context.Block.Type];
                oldBlock.OnDestroy(chunk, pos, globalPos);
                newBlock.OnCreate(chunk, pos, globalPos);

                blocks[context.Index] = context.Block;

                if (context.SetBlockModified)
                    BlockModified(pos, globalPos, context.Block);
                
                if (
                    // Only check neighbors if it is still needed
                    rebuildMask == 0x3f ||
                    // Only check neighbors when it is a change of a block on a chunk's edge
                    (((pos.x+1)&Env.ChunkMask)>1 &&
                     ((pos.y+1)&Env.ChunkMask)>1 &&
                     ((pos.z+1)&Env.ChunkMask)>1)
                    )
                    continue;
                    
                int cx = chunk.pos.x;
                int cy = chunk.pos.y;
                int cz = chunk.pos.z;

                // If it is an edge position, notify neighbor as well
                // Iterate over neighbors and decide which ones should be notified to rebuild
                for (int i = 0; i < chunk.Listeners.Length; i++)
                {
                    ChunkEvent listener = chunk.Listeners[i];
                    if (listener == null)
                        continue;

                    // No further checks needed once we know all neighbors need to be notified
                    if (rebuildMask==0x3f)
                        break;

                    Chunk listenerChunk = (Chunk)listener;

                    int lx = listenerChunk.pos.x;
                    int ly = listenerChunk.pos.y;
                    int lz = listenerChunk.pos.z;

                    if ((ly == cy || lz == cz) &&
                        (
                            // Section to the left
                            ((pos.x == 0) && (lx + Env.ChunkSize == cx)) ||
                            // Section to the right
                            ((pos.x == Env.ChunkMask) && (lx - Env.ChunkSize == cx))
                        ))
                        rebuildMask = rebuildMask | (1 << i);

                    if ((lx == cx || lz == cz) &&
                        (
                            // Section to the bottom
                            ((pos.y == 0) && (ly + Env.ChunkSize == cy)) ||
                            // Section to the top
                            ((pos.y == Env.ChunkMask) && (ly - Env.ChunkSize == cy))
                        ))
                        rebuildMask = rebuildMask | (1 << i);

                    if ((ly == cy || lx == cx) &&
                        (
                            // Section to the back
                            ((pos.z == 0) && (lz + Env.ChunkSize == cz)) ||
                            // Section to the front
                            ((pos.z == Env.ChunkMask) && (lz - Env.ChunkSize == cz))
                        ))
                        rebuildMask = rebuildMask | (1 << i);
                }
            }

            m_setBlockQueue.Clear();

            // Notify neighbors that they need to rebuilt their geometry
            if (rebuildMask > 0)
            {
                for (int j = 0; j < chunk.Listeners.Length; j++)
                {
                    Chunk listener = (Chunk)chunk.Listeners[j];
                    if (listener != null && ((rebuildMask >> j) & 1) != 0)
                        listener.RequestBuildVertices();
                }
            }
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <returns>The block at the position</returns>
        public BlockData Get(BlockPos pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return blocks[index];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <returns>The block at the position</returns>
        public Block GetBlock(BlockPos pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return chunk.world.blockProvider.BlockTypes[blocks[index].Type];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="index">Index to internal block buffer</param>
        /// <returns>The block at the position</returns>
        public Block GetBlock(int index)
        {
            return chunk.world.blockProvider.BlockTypes[blocks[index].Type];
        }

        /// <summary>
        /// Sets the block at the given position
        /// </summary>
        /// <param name="pos">A local block position</param>
        public void Set(BlockPos pos, BlockData blockData)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);

            // Nothing for us to do if block did not change
            BlockData oldBlockData = blocks[index];
            if (oldBlockData.Type==blockData.Type)
                return;

            // Update non-empty block count
            if (blockData.Type==BlockProvider.AirType)
                --NonEmptyBlocks;
            else
                ++NonEmptyBlocks;

            blocks[index] = blockData;
        }

        /// <summary>
        /// Sets the block at the given position
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        public void Modify(BlockPos pos, BlockData blockData, bool setBlockModified)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);

            // Nothing for us to do if block did not change
            BlockData oldBlockData = blocks[index];
            if (oldBlockData.Type==blockData.Type)
                return;

            m_setBlockQueue.Add(new SetBlockContext(index, blockData, setBlockModified));
        }

        public void BlockModified(BlockPos localPos, BlockPos globalPos, BlockData blockData)
        {
            //If this is the server log the changed block so that it can be saved
            if (chunk.world.networking.isServer)
            {
                if (chunk.world.networking.allowConnections)
                    chunk.world.networking.server.BroadcastChange(globalPos, blockData, -1);

                if (!modifiedBlocks.Contains(localPos))
                {
                    modifiedBlocks.Add(localPos);
                    chunk.blocks.contentsModified = true;
                }
            }
            else // if this is not the server send the change to the server to sync
            {
                chunk.world.networking.client.BroadcastChange(globalPos, blockData);
            }
        }

        private bool debugRecieve = false;

        private void InitializeChunkDataReceive(int index, int size)
        {
            receiveIndex = index;
            receiveBuffer = new byte[size];
        }

        public void ReceiveChunkData(byte[] buffer)
        {
            int index = BitConverter.ToInt32(buffer, VmServer.headerSize);
            int size = BitConverter.ToInt32(buffer, VmServer.headerSize+4);
            if (debugRecieve)
                Debug.Log("ChunkBlocks.ReceiveChunkData ("+Thread.CurrentThread.ManagedThreadId+"): "+chunk.pos
                          //+ ", buffer=" + buffer.Length
                          +", index="+index
                          +", size="+size);

            if (receiveBuffer==null)
                InitializeChunkDataReceive(index, size);

            TranscribeChunkData(buffer, VmServer.leaderSize);
        }

        private void TranscribeChunkData(byte[] buffer, int offset)
        {
            for (int o = offset; o<buffer.Length; o++)
            {
                receiveBuffer[receiveIndex] = buffer[o];
                receiveIndex++;

                if (receiveIndex==receiveBuffer.Length)
                {
                    if (debugRecieve)
                        Debug.Log("ChunkBlocks.TranscribeChunkData ("+Thread.CurrentThread.ManagedThreadId+"): "+
                                  chunk.pos
                                  +", receiveIndex="+receiveIndex);

                    FinishChunkDataReceive();
                    return;
                }
            }
        }

        private void FinishChunkDataReceive()
        {
            GenerateContentsFromBytes();

            Chunk.OnGenerateDataOverNetworkDone(chunk);

            receiveBuffer = null;
            receiveIndex = 0;

            if (debugRecieve)
                Debug.Log("ChunkBlocks.FinishChunkDataReceive ("+Thread.CurrentThread.ManagedThreadId+"): "+chunk.pos);
        }

        public byte[] ToBytes()
        {
            List<byte> buffer = new List<byte>();
            BlockData blockData;
            BlockData lastBlockData = new BlockData(1);

            byte[] data;
            short sameBlockCount = 0;
            int countIndex = 0;

            for (int y = 0; y<Env.ChunkSize; y++)
            {
                for (int z = 0; z<Env.ChunkSize; z++)
                {
                    for (int x = 0; x<Env.ChunkSize; x++)
                    {
                        int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                        blockData = blocks[index];

                        if (blockData.Equals(lastBlockData))
                        {
                            //if this is the same as the last block added increase the count
                            ++sameBlockCount;
                            byte[] shortAsBytes = BitConverter.GetBytes(sameBlockCount);
                            buffer[countIndex] = shortAsBytes[0];
                            buffer[countIndex+1] = shortAsBytes[1];
                        }
                        else
                        {
                            BlockData bd = new BlockData(blockData.Type);
                            data = bd.ToByteArray();

                            //Add 1 as a short (2 bytes) 
                            countIndex = buffer.Count;
                            sameBlockCount = 1;
                            buffer.AddRange(BitConverter.GetBytes(1));
                            //Then add the block data
                            buffer.AddRange(data);

                            lastBlockData = blockData;
                        }

                    }
                }
            }

            return buffer.ToArray();
        }

        private void GenerateContentsFromBytes()
        {
            int i = 0;
            BlockData blockData = new BlockData(0);
            short blockCount = 0;

            for (int y = 0; y<Env.ChunkSize; y++)
            {
                for (int z = 0; z<Env.ChunkSize; z++)
                {
                    for (int x = 0; x<Env.ChunkSize; x++)
                    {
                        if (blockCount==0)
                        {
                            blockCount = BitConverter.ToInt16(receiveBuffer, i);
                            i += 2;
                            
                            ushort type = BitConverter.ToUInt16(receiveBuffer, i);
                            blockData = new BlockData(type);
                            i += 2;
                            i += blockData.RestoreBlockData(receiveBuffer, i);
                        }

                        Set(new BlockPos(x, y, z), blockData);
                        blockCount--;
                    }
                }
            }
        }
    }
}