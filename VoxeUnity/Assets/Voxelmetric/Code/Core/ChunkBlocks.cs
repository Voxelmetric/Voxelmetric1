using System;
using System.Collections.Generic;
using UnityEngine.Assertions;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.VM;

namespace Voxelmetric.Code.Core
{
    public sealed class ChunkBlocks
    {
        public Chunk chunk { get; private set; }

        private Block[] m_blockTypes;

        //! Array of block data
        private readonly BlockData[] blocks = Helpers.CreateArray1D<BlockData>(Env.ChunkSizeWithPaddingPow3);
        
        //! Compressed array of block data
        private readonly List<BlockDataAABB> blockCompressed = new List<BlockDataAABB>();
        public List<BlockDataAABB> BlocksCompressed
        {
            get { return blockCompressed; }
        }

        //! Number of blocks which are not air (non-empty blocks)
        public int NonEmptyBlocks;

        //! Queue of setBlock operations to execute
        private readonly List<SetBlockContext> m_setBlockQueue = new List<SetBlockContext>();

        private byte[] receiveBuffer;
        private int receiveIndex;

        private long lastUpdateTimeGeometry;
        private long lastUpdateTimeCollider;
        private int rebuildMaskGeometry;
        private int rebuildMaskCollider;

        public readonly List<BlockPos> modifiedBlocks = new List<BlockPos>();

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
            Array.Clear(blocks, 0, blocks.Length);
        }

        public void Init()
        {
            m_blockTypes = chunk.world.blockProvider.BlockTypes;
        }

        public void Copy(ChunkBlocks src, int srcIndex, int dstIndex, int length)
        {
            Array.Copy(src.blocks, srcIndex, blocks, dstIndex, length);
        }
        
        public void Reset()
        {
            NonEmptyBlocks = -1;
            // Reset internal parts of the chunk buffer. Edges not touched
            Array.Clear(blocks, 0, Env.ChunkSizeWithPaddingPow3);

            lastUpdateTimeGeometry = 0;
            lastUpdateTimeCollider = 0;
            rebuildMaskGeometry = -1;
            rebuildMaskCollider = -1;

            modifiedBlocks.Clear();
        }

        public void RequestCollider()
        {
            // Request collider update if there is no request yet
            if (rebuildMaskCollider<0)
                rebuildMaskCollider = 0;
        }

        public void CalculateEmptyBlocks()
        {
            if (NonEmptyBlocks>=0)
                return;
            NonEmptyBlocks = 0;

            for (int y = 0; y<Env.ChunkSize; y++)
            {
                for (int z = 0; z<Env.ChunkSize; z++)
                {
                    for (int x = 0; x<Env.ChunkSize; x++)
                    {
                        int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                        if (blocks[index].Type!=BlockProvider.AirType)
                            ++NonEmptyBlocks;
                    }
                }
            }
        }

        private void ProcessSetBlockQueue(BlockData block, int index, bool setBlockModified)
        {
            int x, y, z;
            Helpers.GetChunkIndex3DFrom1D(index, out x, out y, out z);

#if DEBUG
            if (x < 0 || y < 0 || z < 0 || x > Env.ChunkSize1 || y > Env.ChunkSize1 || z > Env.ChunkSize1)
            {
                Assert.IsTrue(false, string.Format("Chunk index out of range in setBlockQueue: [{0},{1},{2}]", x, y, z));
                return;
            }
#endif

            Vector3Int pos = new Vector3Int(x, y, z);
            Vector3Int globalPos = pos + chunk.pos;

            BlockData oldBlockData = blocks[index];

            Block oldBlock = m_blockTypes[oldBlockData.Type];
            Block newBlock = m_blockTypes[block.Type];
            oldBlock.OnDestroy(chunk, ref pos);
            newBlock.OnCreate(chunk, ref pos);

            SetInner(index, block);

            if (setBlockModified)
            {
                BlockModified(new BlockPos(x, y, z), ref globalPos, block);
            }

            if (
                // Only check neighbors if it is still needed
                rebuildMaskGeometry==0x3f ||
                // Only check neighbors when it is a change of a block on a chunk's edge
                (pos.x>0 && pos.x<Env.ChunkSize1 &&
                 pos.y>0 && pos.y<Env.ChunkSize1 &&
                 pos.z>0 && pos.z<Env.ChunkSize1)
                )
                return;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;

            ChunkStateManagerClient stateManager = chunk.stateManager;

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            for (int i = 0; i < stateManager.Listeners.Length; i++)
            {
                ChunkEvent listener = stateManager.Listeners[i];
                if (listener == null)
                    continue;

                // No further checks needed once we know all neighbors need to be notified
                if (rebuildMaskGeometry == 0x3f)
                    break;

                ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
                Chunk listenerChunk = listenerClient.chunk;

                int lx = listenerChunk.pos.x;
                int ly = listenerChunk.pos.y;
                int lz = listenerChunk.pos.z;

                if (ly == cy || lz == cz)
                {
                    // Section to the left
                    if ((pos.x == 0) && (lx + Env.ChunkSize == cx))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(Env.ChunkSize, y, z);
                        listenerChunk.blocks.blocks[neighborIndex] = block;
                    }
                    // Section to the right
                    else if ((pos.x == Env.ChunkSize1) && (lx - Env.ChunkSize == cx))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(-1, y, z);
                        listenerChunk.blocks.blocks[neighborIndex] = block;
                    }
                }

                if (lx == cx || lz == cz)
                {
                    // Section to the bottom
                    if ((pos.y == 0) && (ly + Env.ChunkSize == cy))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(x, Env.ChunkSize, z);
                        listenerChunk.blocks.blocks[neighborIndex] = block;
                    }
                    // Section to the top
                    else if ((pos.y == Env.ChunkSize1) && (ly - Env.ChunkSize == cy))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(x, -1, z);
                        listenerChunk.blocks.blocks[neighborIndex] = block;
                    }
                }

                if (ly == cy || lx == cx)
                {
                    // Section to the back
                    if ((pos.z == 0) && (lz + Env.ChunkSize == cz))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(x, y, Env.ChunkSize);
                        listenerChunk.blocks.blocks[neighborIndex] = block;
                    }
                    // Section to the front
                    else if ((pos.z == Env.ChunkSize1) && (lz - Env.ChunkSize == cz))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(x, y, -1);
                        listenerChunk.blocks.blocks[neighborIndex] = block;
                    }
                }
            }
        }

        public void Update()
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;

            if (m_setBlockQueue.Count>0)
            {
                if (rebuildMaskGeometry<0)
                    rebuildMaskGeometry = 0;
                if (rebuildMaskCollider<0)
                    rebuildMaskCollider = 0;

                // Modify blocks
                for (int j = 0; j<m_setBlockQueue.Count; j++)
                {
                    SetBlockContext context = m_setBlockQueue[j];

                    if (!context.IsRange())
                    {
                        ProcessSetBlockQueue(context.Block, context.IndexFrom, context.SetBlockModified);

                        if (context.ParentContext != null)
                            context.ParentContext.ChildActionFinished();
                    }
                    else
                    {
                        int sx, sy, sz, ex, ey, ez;
                        Helpers.GetChunkIndex3DFrom1D(context.IndexFrom, out sx, out sy, out sz);
                        Helpers.GetChunkIndex3DFrom1D(context.IndexTo, out ex, out ey, out ez);

                        for (int y = sy; y<=ey; y++)
                        {
                            for (int z = sz; z<=ez; z++)
                            {
                                for (int x = sx; x<=ex; x++)
                                {
                                    int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                                    ProcessSetBlockQueue(context.Block, index, context.SetBlockModified);

                                    if (context.ParentContext != null)
                                        context.ParentContext.ChildActionFinished();
                                }
                            }
                        }
                    }
                }

                rebuildMaskCollider |= rebuildMaskGeometry;

                m_setBlockQueue.Clear();
            }

            long now = Globals.Watch.ElapsedMilliseconds;

            // Request a geometry update at most 10 times a second
            if (rebuildMaskGeometry>=0 && now-lastUpdateTimeGeometry>=100)
            {
                lastUpdateTimeGeometry = now;

                // Request rebuild on this chunk
                stateManager.RequestState(ChunkState.BuildVerticesNow);

                // Notify neighbors that they need to rebuilt their geometry
                if (rebuildMaskGeometry>0)
                {
                    for (int j = 0; j<stateManager.Listeners.Length; j++)
                    {
                        ChunkStateManagerClient listener = (ChunkStateManagerClient)stateManager.Listeners[j];
                        if (listener!=null && ((rebuildMaskGeometry>>j)&1)!=0)
                        {
                            // Request rebuild on neighbor chunks
                            listener.RequestState(ChunkState.BuildVerticesNow);
                        }
                    }
                }

                rebuildMaskGeometry = -1;
            }

            // Request a collider update at most 4 times a second
            if (chunk.NeedsCollider && rebuildMaskCollider>=0 && now-lastUpdateTimeCollider>=250)
            {
                lastUpdateTimeCollider = now;

                // Request rebuild on this chunk
                stateManager.RequestState(ChunkState.BuildCollider);

                // Notify neighbors that they need to rebuilt their geometry
                if (rebuildMaskCollider > 0)
                {
                    for (int j = 0; j < stateManager.Listeners.Length; j++)
                    {
                        ChunkStateManagerClient listener = (ChunkStateManagerClient)stateManager.Listeners[j];
                        if (listener != null && ((rebuildMaskCollider >> j) & 1) != 0)
                        {
                            // Request rebuild on neighbor chunks
                            if (listener.chunk.NeedsCollider)
                                listener.RequestState(ChunkState.BuildCollider);
                        }
                    }
                }

                rebuildMaskCollider = -1;
            }
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="pos">Position in local chunk coordinates</param>
        /// <returns>The block at the position</returns>
        public BlockData Get(ref Vector3Int pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return blocks[index];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="pos">Position in local chunk coordinates</param>
        /// <returns>The block at the position</returns>
        public Block GetBlock(ref Vector3Int pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            return m_blockTypes[blocks[index].Type];
        }

        /// <summary>
        /// Returns block data from a position within the chunk
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <returns>The block at the position</returns>
        public BlockData Get(int index)
        {
            return blocks[index];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <returns>The block at the position</returns>
        public Block GetBlock(int index)
        {
            return m_blockTypes[blocks[index].Type];
        }

        /// <summary>
        /// Sets the block at the given position. The position is guaranteed to be inside chunk's non-padded area
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetInner(int index, BlockData blockData)
        {
            // Nothing for us to do if there was no change
            BlockData oldBlockData = blocks[index];
            ushort type = blockData.Type;
            if (oldBlockData.Type==type)
                return;

            if (type==BlockProvider.AirType)
                --NonEmptyBlocks;
            else if (oldBlockData.Type == BlockProvider.AirType)
                ++NonEmptyBlocks;

            blocks[index] = blockData;
        }

        /// <summary>
        /// Sets the block at the given position. It does not perform any logic. It simply sets the block.
        /// Use this function only when generating the terrain and structures.
        /// </summary>
        /// <param name="index">Index in local chunk data</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRaw(int index, BlockData blockData)
        {
            blocks[index] = blockData;
        }

        /// <summary>
        /// Sets blocks to a given value in a given range
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRange(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            for (int y = posFrom.y; y<=posTo.y; y++)
            {
                for (int z = posFrom.z; z<=posTo.z; z++)
                {
                    for (int x = posFrom.x; x<=posTo.x; x++)
                    {
                        SetInner(Helpers.GetChunkIndex1DFrom3D(x, y, z), blockData);
                    }
                }
            }
        }

        /// <summary>
        /// Sets blocks to a given value in a given range. It does not perform any logic. It simply sets the blocks.
        /// Use this function only when generating the terrain and structures.
        /// </summary>
        /// <param name="posFrom">Starting position in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">A block to be placed on a given position</param>
        public void SetRangeRaw(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData)
        {
            for (int y = posFrom.y; y <= posTo.y; y++)
            {
                for (int z = posFrom.z; z <= posTo.z; z++)
                {
                    for (int x = posFrom.x; x <= posTo.x; x++)
                    {
                        SetRaw(Helpers.GetChunkIndex1DFrom3D(x, y, z), blockData);
                    }
                }
            }
        }

        /// <summary>
        /// Queues a modification of a block on a given position
        /// </summary>
        /// <param name="pos">Position in local chunk coordinates</param>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="parentContext">Context of a parent which performed this operation</param>
        public void Modify(ref Vector3Int pos, BlockData blockData, bool setBlockModified, ModifyBlockContext parentContext=null)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z);
            
            m_setBlockQueue.Add(new SetBlockContext(index, blockData, setBlockModified, parentContext));

            if (parentContext != null)
                parentContext.RegisterChildAction();
        }

        /// <summary>
        /// Queues a modification of blocks in a given range
        /// </summary>
        /// <param name="posFrom">Starting positon in local chunk coordinates</param>
        /// <param name="posTo">Ending position in local chunk coordinates</param>
        /// <param name="blockData">BlockData to place at the given location</param>
        /// <param name="setBlockModified">Set to true to mark chunk data as modified</param>
        /// <param name="parentContext">Context of a parent which performed this operation</param>
        public void ModifyRange(ref Vector3Int posFrom, ref Vector3Int posTo, BlockData blockData, bool setBlockModified, ModifyBlockContext parentContext=null)
        {
            int indexFrom = Helpers.GetChunkIndex1DFrom3D(posFrom.x, posFrom.y, posFrom.z);
            int indexTo = Helpers.GetChunkIndex1DFrom3D(posTo.x, posTo.y, posTo.z);

            m_setBlockQueue.Add(new SetBlockContext(indexFrom, indexTo, blockData, setBlockModified, parentContext));

            if (parentContext!=null)
                parentContext.RegisterChildAction();
        }

        public void BlockModified(BlockPos blockPos, ref Vector3Int globalPos, BlockData blockData)
        {
            // If this is the server log the changed block so that it can be saved
            if (chunk.world.networking.isServer)
            {
                if (chunk.world.networking.allowConnections)
                    chunk.world.networking.server.BroadcastChange(globalPos, blockData, -1);

                if (Features.UseSerialization && Features.UseDifferentialSerialization)
                {
                    // TODO: Memory unfriendly. Rethink the strategy
                    modifiedBlocks.Add(blockPos);
                }
            }
            else // if this is not the server send the change to the server to sync
            {
                chunk.world.networking.client.BroadcastChange(globalPos, blockData);
            }
        }

        private void InitializeChunkDataReceive(int index, int size)
        {
            receiveIndex = index;
            receiveBuffer = new byte[size];
        }

        public void ReceiveChunkData(byte[] buffer)
        {
            int index = BitConverter.ToInt32(buffer, VmServer.headerSize);
            int size = BitConverter.ToInt32(buffer, VmServer.headerSize+4);

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
                    FinishChunkDataReceive();
                    return;
                }
            }
        }

        private void FinishChunkDataReceive()
        {
            if(!InitFromBytes())
                Reset();

            ChunkStateManagerClient stateManager = chunk.stateManager;
            ChunkStateManagerClient.OnGenerateDataOverNetworkDone(stateManager);

            receiveBuffer = null;
            receiveIndex = 0;
        }

        #region Compression

        private bool ExpandX(ref bool[] mask, ushort type, int y1, int z1, ref int x2, int y2, int z2)
        {
            // Check the quad formed by YZ axes and try to expand the X asix
            for (int y = y1; y<y2; ++y)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x2, y, z1);
                for (int z = z1; z<z2; ++z, index += Env.ChunkSizeWithPadding)
                {
                    if (mask[index] || blocks[index].Type!=type)
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

        private bool ExpandY(ref bool[] mask, ushort type, int x1, int z1, int x2, ref int y2, int z2)
        {
            // Check the quad formed by XZ axes and try to expand the Y asix
            for (int z = z1; z<z2; ++z)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x1, y2, z);
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || blocks[index].Type!=type)
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

        private bool ExpandZ(ref bool[] mask, ushort type, int x1, int y1, int x2, int y2, ref int z2)
        {
            // Check the quad formed by XY axes and try to expand the Z asix
            for (int y = y1; y<y2; ++y)
            {
                int index = Helpers.GetChunkIndex1DFrom3D(x1, y, z2);
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || blocks[index].Type!=type)
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

        /// <summary>
        /// Compresses chunk's memory.
        /// </summary>
        public void Compress()
        {
            var pools = chunk.pools;
            bool[] mask = pools.BoolArrayPool.PopExact(Env.ChunkSizeWithPaddingPow3);

            Array.Clear(mask, 0, mask.Length);
            blockCompressed.Clear();

            // This compression is essentialy RLE. However, instead of working on 1 axis
            // it works in 3 dimensions.
            int index = 0;
            for (int y = -1; y<Env.ChunkSizePlusPadding; ++y)
            {
                for (int z = -1; z<Env.ChunkSizePlusPadding; ++z)
                {
                    for (int x = -1; x<Env.ChunkSizePlusPadding; ++x, ++index)
                    {
                        // Skip already checked blocks
                        if (mask[index])
                            continue;

                        mask[index] = true;

                        // Skip air data
                        ushort data = blocks[index].Data;
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

                            if (expandX)
                            {
                                expandX = x2<Env.ChunkSizePlusPadding &&
                                          ExpandX(ref mask, type, y1, z1, ref x2, y2, z2);
                                expand = expandX;
                            }
                            if (expandY)
                            {
                                expandY = y2<Env.ChunkSizePlusPadding &&
                                          ExpandY(ref mask, type, x1, z1, x2, ref y2, z2);
                                expand = expand|expandY;
                            }
                            if (expandZ)
                            {
                                expandZ = z2<Env.ChunkSizePlusPadding &&
                                          ExpandZ(ref mask, type, x1, y1, x2, y2, ref z2);
                                expand = expand|expandZ;
                            }
                        } while (expand);

                        blockCompressed.Add(new BlockDataAABB(data, x1, y1, z1, x2, y2, z2));

                        /*// Let's make sure that we don't take too much space
                        int compressedSize = blockCompressed.Count*StructSerialization.TSSize<BlockDataAABB>.ValueSize;
                        int decompressedSize = Env.ChunkSizeWithPaddingPow3*StructSerialization.TSSize<BlockData>.ValueSize;
                        if (compressedSize>=(decompressedSize>>1))
                        {
                            blockCompressed.Clear();
                            pools.BoolArrayPool.Push(mask);
                            return;
                        }*/
                    }
                }
            }

            pools.BoolArrayPool.Push(mask);
        }

        /// <summary>
        /// Decompresses chunk's memory.
        /// </summary>
        public void Decompress()
        {
            for (int i = 0; i<blockCompressed.Count; ++i)
            {
                var box = blockCompressed[i];
                int x1 = box.MinX;
                int y1 = box.MinY;
                int z1 = box.MinZ;
                int x2 = box.MaxX;
                int y2 = box.MaxY;
                int z2 = box.MaxZ;
                ushort data = box.Data;

                for (int y = y1; y<y2; ++y)
                {
                    for (int z = z1; z<z2; ++z)
                    {
                        for (int x = x1; x<x2; ++x)
                        {
                            int index = Helpers.GetChunkIndex1DFrom3D(x, y, z);
                            blocks[index] = new BlockData(data);
                        }
                    }
                }
            }
        }

        #endregion

        #region "Temporary network compression solution"

        public byte[] ToBytes()
        {
            List<byte> buffer = new List<byte>();
            buffer.AddRange(BitConverter.GetBytes(NonEmptyBlocks));
            if (NonEmptyBlocks > 0)
            {
                int sameBlockCount = 1;
                BlockData lastBlockData = blocks[0];

                for (int index = 1; index < Env.ChunkSizeWithPaddingPow3; index++)
                {
                    if (blocks[index].Equals(lastBlockData))
                    {
                        // If this is the same as the last block added increase the count
                        ++sameBlockCount;
                    }
                    else
                    {
                        buffer.AddRange(BitConverter.GetBytes(sameBlockCount));
                        buffer.AddRange(BlockData.ToByteArray(lastBlockData));

                        sameBlockCount = 1;
                        lastBlockData = blocks[index];
                    }
                }

                buffer.AddRange(BitConverter.GetBytes(sameBlockCount));
                buffer.AddRange(BlockData.ToByteArray(lastBlockData));
            }

            return buffer.ToArray();
        }

        private bool InitFromBytes()
        {
            if (receiveBuffer == null || receiveBuffer.Length < 4)
                return false;

            NonEmptyBlocks = BitConverter.ToInt32(receiveBuffer, 0);
            if (NonEmptyBlocks > 0)
            {
                int dataOffset = 4;
                int blockOffset = 0;

                while (dataOffset < receiveBuffer.Length)
                {
                    int sameBlockCount = BitConverter.ToInt32(receiveBuffer, dataOffset + 4); // 4 bytes
                    BlockData bd = new BlockData(BlockData.RestoreBlockData(receiveBuffer, dataOffset + 8)); // 2 bytes
                    for (int i = blockOffset; i < blockOffset + sameBlockCount; i++)
                        blocks[i] = bd;

                    dataOffset += 4 + 2;
                    blockOffset += sameBlockCount;
                }
            }

            return true;
        }

        #endregion
    }
}