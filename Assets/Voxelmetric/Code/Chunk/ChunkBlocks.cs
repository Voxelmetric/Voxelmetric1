using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using System;
using System.Diagnostics;

public class ChunkBlocks {

    protected Chunk chunk;

    protected Block[,,] blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];
    public List<BlockPos> modifiedBlocks = new List<BlockPos>();
    protected byte[] receiveBuffer;
    protected int receiveIndex;
    public bool contentsGenerated;
    public bool generationStarted;
    Stopwatch sw;

    public ChunkBlocks(Chunk chunk)
    {
        this.chunk = chunk;
    }

    public void ResetContent()
    {
        blocks = new Block[Config.Env.ChunkSize, Config.Env.ChunkSize, Config.Env.ChunkSize];
        contentsGenerated = false;
        generationStarted = false;
    }

    /// <summary>
    /// Gets and returns a block from a position within the chunk 
    /// or fetches it from the world
    /// </summary>
    /// <param name="blockPos">A global block position</param>
    /// <returns>The block at the position</returns>
    public virtual Block Get(BlockPos blockPos)
    {
        if (InRange(blockPos))
        {
            return LocalGet(blockPos - chunk.pos);
        }
        else
        {
            return chunk.world.blocks.Get(blockPos);
        }
    }

    /// <summary>
    /// This function takes a block position relative to the chunk's position. It is slightly faster
    /// than the GetBlock function so use this if you already have a local position available otherwise
    /// use GetBlock. If the position is lesser or greater than the size of the chunk it will get the value
    /// from the chunk containing the block pos
    /// </summary>
    /// <param name="localBlockPos"> A block pos relative to the chunk's position. MUST be a local position or the wrong block will be returned</param>
    /// <returns>the block at the relative position</returns>
    public virtual Block LocalGet(BlockPos localBlockPos)
    {
        if ((localBlockPos.x < Config.Env.ChunkSize && localBlockPos.x >= 0) &&
            (localBlockPos.y < Config.Env.ChunkSize && localBlockPos.y >= 0) &&
            (localBlockPos.z < Config.Env.ChunkSize && localBlockPos.z >= 0))
        {
            Block block = blocks[localBlockPos.x, localBlockPos.y, localBlockPos.z];
            if (block == null)
            {
                return Block.Air;
            }
            else
            {
                return block;
            }
        }
        else
        {
            return chunk.world.blocks.Get(localBlockPos + chunk.pos);
        }
    }

    public virtual void Set(BlockPos blockPos, string block, bool updateChunk = true, bool setBlockModified = true)
    {
        Set(blockPos, Block.New(block, chunk.world), updateChunk, setBlockModified);
    }

    /// <summary> Sets the block at the given position </summary>
    /// <param name="blockPos">Block position</param>
    /// <param name="block">Block to place at the given location</param>
    /// <param name="updateChunk">Optional parameter, set to false to keep the chunk unupdated despite the change</param>
    public virtual void Set(BlockPos blockPos, Block block, bool updateChunk = true, bool setBlockModified = true)
    {
        if (InRange(blockPos))
        {
            //Only call create and destroy if this is a different block type, otherwise it's just updating the properties of an existing block
            if (Get(blockPos).type != block.type)
            {
                Get(blockPos).OnDestroy(chunk, blockPos, blockPos + chunk.pos);
                block.OnCreate(chunk, blockPos, blockPos + chunk.pos);
            }

            blocks[blockPos.x - chunk.pos.x, blockPos.y - chunk.pos.y, blockPos.z - chunk.pos.z] = block;

            if (setBlockModified)
                BlockModified(blockPos);

            if (updateChunk)
                chunk.UpdateNow();
        }
        else
        {
            //if the block is out of range set it through world
            chunk.world.blocks.Set(blockPos, block, updateChunk);
        }
    }

    /// <summary>
    /// This function takes a block position relative to the chunk's position. It is slightly faster
    /// than the SetBlock function so use this if you already have a local position available otherwise
    /// use SetBlock. If the position is lesser or greater than the size of the chunk it will call setblock
    /// using the world.
    /// </summary>
    /// <param name="blockPos"> A block pos relative to the chunk's position.</param>
    public virtual void LocalSet(BlockPos blockPos, Block block)
    {
        if ((blockPos.x < Config.Env.ChunkSize && blockPos.x >= 0) &&
            (blockPos.y < Config.Env.ChunkSize && blockPos.y >= 0) &&
            (blockPos.z < Config.Env.ChunkSize && blockPos.z >= 0))
        {
            blocks[blockPos.x, blockPos.y, blockPos.z] = block;
        }
    }

    /// <summary> Returns true if the block local block position is contained in the chunk boundaries </summary>
    /// <param name="blockPos">A block position</param>
    public bool InRange(BlockPos blockPos)
    {
        return (blockPos.ContainingChunkCoordinates() == chunk.pos);
    }

    public void BlockModified(BlockPos pos)
    {
        //If this is the server log the changed block so that it can be saved
        if (chunk.world.isServer)
        {
            if (chunk.world.allowConnections)
            {
                chunk.world.server.BroadcastChange(pos, Get(pos), -1);
            }

            if (!modifiedBlocks.Contains(pos))
            {
                if (!modifiedBlocks.Contains(pos))
                {
                    modifiedBlocks.Add(pos);
                    chunk.logic.SetFlag(Flag.chunkModified, true);
                }
            }
        }
        else // if this is not the server send the change to the server to sync
        {
            chunk.world.client.BroadcastChange(pos, Get(pos));
        }
    }

    public void GenerateChunkContents()
    {
        if (contentsGenerated)
        {
            return;
        }

        if (chunk.world.isServer)
        {
            chunk.world.terrainGen.GenerateTerrainForChunk(chunk);
            Serialization.Load(chunk);

            contentsGenerated = true;
        }
        else
        {
            if (!generationStarted)
            {
                generationStarted = true;
                sw.Start();
                chunk.world.client.RequestChunk(chunk.pos);
            }
        }
    }


    void InitializeChunkDataReceive(int size)
    {
        receiveBuffer = new byte[size];
        receiveIndex = 0;
    }

    public void ReceiveChunkData(byte[] buffer)
    {
        if (receiveBuffer == null)
        {
            InitializeChunkDataReceive(BitConverter.ToInt32(buffer, 13));
        }
        TranscribeChunkData(buffer, 17);
    }

    void TranscribeChunkData(byte[] buffer, int offset)
    {
        for (int o = offset; o < buffer.Length; o++) {
            receiveBuffer[receiveIndex] = buffer[o];
            receiveIndex++;

            if (receiveIndex == receiveBuffer.Length)
            {
                FinishChunkDataReceive();
                return;
            }
        }
    }

    void FinishChunkDataReceive()
    {
        UnityEngine.Debug.Log(sw.Elapsed);
        sw.Stop();

        GenerateContentsFromBytes();
        contentsGenerated = true;
        receiveBuffer = null;
        receiveIndex = 0;
    }

    public byte[] ToBytes()
    {
        List<byte> buffer = new List<byte>();
        Block block;
        for (int x = 0; x < Config.Env.ChunkSize; x++)
        {
            for (int y = 0; y < Config.Env.ChunkSize; y++)
            {
                for (int z = 0; z < Config.Env.ChunkSize; z++)
                {
                    block = LocalGet(new BlockPos(x, y, z));
                    buffer.AddRange(block.ToByteArray());
                    //later must also byte the blocks data and add it
                }
            }
        }

        return buffer.ToArray();
    }

    void GenerateContentsFromBytes()
    {
        int i = 0;
        Block block;
        for (int x = 0; x < Config.Env.ChunkSize; x++)
        {
            for (int y = 0; y < Config.Env.ChunkSize; y++)
            {
                for (int z = 0; z < Config.Env.ChunkSize; z++)
                {
                    block = Block.New(BitConverter.ToUInt16(receiveBuffer, i), chunk.world);
                    i += 2;
                    i += block.RestoreBlockData(receiveBuffer, i);
                    LocalSet(new BlockPos(x, y, z), block);
                }
            }
        }
    }
}
