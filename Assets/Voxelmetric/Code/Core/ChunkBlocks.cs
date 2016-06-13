using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using UnityEngine;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;
using Voxelmetric.Code.VM;

namespace Voxelmetric.Code.Core
{
    public sealed class ChunkBlocks
    {
        private Chunk chunk;
        private readonly BlockData[] blocks = Helpers.CreateArray1D<BlockData>(Env.ChunkVolume);
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
        }

        /// <summary>
        /// Gets and returns block data at a position within the chunk
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <returns>The block at the position</returns>
        public BlockData Get(BlockPos pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return blocks[index];
        }

        /// <summary>
        /// Gets and returns a block from a position within the chunk
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <returns>The block at the position</returns>
        public Block GetBlock(BlockPos pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return chunk.world.blockProvider.BlockTypes[blocks[index].Type];
        }

        /// <summary>
        /// Sets the block at the given position
        /// </summary>
        /// <param name="pos">A local block position</param>
        public void Set(BlockPos pos, BlockData blockData)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            blocks[index] = blockData;
        }

        /// <summary>
        /// Sets the block at the given position
        /// </summary>
        /// <param name="pos">A local block position</param>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="updateChunk">Optional parameter, set to false to keep the chunk unupdated despite the change</param>
        /// <param name="setBlockModified">Optional parameter, set to true to mark chunk data as modified</param>
        public void Modify(BlockPos pos, BlockData blockData, bool updateChunk = true, bool setBlockModified = true)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);

            BlockPos globalPos = pos+chunk.pos;

            //Only call create and destroy if this is a different block type, otherwise it's just updating the properties of an existing block
            BlockData oldBlockData = blocks[index];
            if (oldBlockData.Type != blockData.Type)
            {
                Block oldBlock = chunk.world.blockProvider.BlockTypes[oldBlockData.Type];
                Block newBlock = chunk.world.blockProvider.BlockTypes[blockData.Type];
                oldBlock.OnDestroy(chunk, pos, globalPos);
                newBlock.OnCreate(chunk, pos, globalPos);
            }

            blocks[index] = blockData;
            
            // TODO: Queue changes
            if (setBlockModified)
                BlockModified(pos, globalPos, blockData);

            if (updateChunk)
            {
                chunk.RequestBuildVertices();

                // If it is an edge position, notify neighbor as well
                // Iterate over neighbors and decide which ones should be notified to rebuild
                for (int i = 0; i < chunk.Listeners.Length; i++)
                {
                    ChunkEvent listener = chunk.Listeners[i];
                    if (listener == null)
                        continue;
                    
                    // TODO: Only notify neighbors that really need it
                    Chunk listenerChunk = (Chunk)listener;
                    listenerChunk.RequestBuildVertices();
                }
            }
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