using System;
using System.Collections.Generic;
using System.Diagnostics;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Core.Operations;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Load_Resources.Blocks;
using Voxelmetric.Code.VM;
using Assert = UnityEngine.Assertions.Assert;

namespace Voxelmetric.Code.Core
{
    public sealed class ChunkBlocks
    {
        public Chunk chunk { get; private set; }

        private Block[] m_blockTypes;

        private readonly int m_sideSize = 0;
        private readonly int m_pow = 0;

        //! Array of block data
        private readonly BlockData[] blocks;
        
        //! Compressed array of block data
        private readonly List<BlockDataAABB> blockCompressed = new List<BlockDataAABB>();
        public List<BlockDataAABB> BlocksCompressed
        {
            get { return blockCompressed; }
        }

        //! Number of blocks which are not air (non-empty blocks)
        public int NonEmptyBlocks;

        //! Queue of setBlock operations to execute
        private readonly List<ModifyOp> m_setBlockQueue = new List<ModifyOp>();

        private byte[] receiveBuffer;
        private int receiveIndex;

        private long lastUpdateTimeGeometry;
        private long lastUpdateTimeCollider;
        private int rebuildMaskGeometry;
        private int rebuildMaskCollider;

        public List<BlockPos> modifiedBlocks = new List<BlockPos>();

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

        public ChunkBlocks(Chunk chunk, int sideSize)
        {
            this.chunk = chunk;

            m_sideSize = sideSize;
            m_pow = 1 + (int)Math.Log(sideSize, 2);

            sideSize = m_sideSize + Env.ChunkPadding2;
            blocks = Helpers.CreateArray1D<BlockData>(sideSize * sideSize * sideSize);
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
            // Reset internal parts of the chunk buffer
            Array.Clear(blocks, 0, blocks.Length);

            lastUpdateTimeGeometry = 0;
            lastUpdateTimeCollider = 0;
            rebuildMaskGeometry = -1;
            rebuildMaskCollider = -1;

            // We have to reallocate the list. Otherwise, the array could potentially grow
            // to Env.ChunkSizePow3 size.
            if (modifiedBlocks==null || modifiedBlocks.Count>m_sideSize*3) // Reallocation threshold
                modifiedBlocks = new List<BlockPos>();
            else
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

            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int index = Env.ChunkPadding + (Env.ChunkPadding << m_pow) + (Env.ChunkPadding << (m_pow << 1));
            int yOffset = sizeWithPaddingPow2 - m_sideSize * sizeWithPadding;
            int zOffset = sizeWithPadding - m_sideSize;
            for (int y = 0; y<m_sideSize; ++y, index+=yOffset)
            {
                for (int z = 0; z<m_sideSize; ++z, index+=zOffset)
                {
                    for (int x = 0; x<m_sideSize; ++x, ++index)
                    {
                        if (blocks[index].Type!=BlockProvider.AirType)
                            ++NonEmptyBlocks;
                    }
                }
            }
        }

        public bool NeedToHandleNeighbors(ref Vector3Int pos)
        {
            return rebuildMaskGeometry!=0x3f &&
                   // Only check neighbors when it is a change of a block on a chunk's edge
                   (pos.x<=0 || pos.x>=(m_sideSize-1) ||
                    pos.y<=0 || pos.y>=(m_sideSize-1) ||
                    pos.z<=0 || pos.z>=(m_sideSize-1));
        }

        private ChunkBlocks HandleNeighborRight(ref Vector3Int pos)
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            int i = DirectionUtils.Get(Direction.east);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var listeners = stateManager.Listeners;
            ChunkEvent listener = listeners[i];
            if (listener == null)
                return null;

            ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
            Chunk listenerChunk = listenerClient.chunk;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;
            int lx = listenerChunk.pos.x;
            int ly = listenerChunk.pos.y;
            int lz = listenerChunk.pos.z;

            if (ly!=cy && lz!=cz)
                return null;

            if ((pos.x!=(m_sideSize-1)) || (lx-m_sideSize!=cx))
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return listenerChunk.blocks;
        }

        private ChunkBlocks HandleNeighborLeft(ref Vector3Int pos)
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            int i = DirectionUtils.Get(Direction.west);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var listeners = stateManager.Listeners;
            ChunkEvent listener = listeners[i];
            if (listener == null)
                return null;

            ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
            Chunk listenerChunk = listenerClient.chunk;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;
            int lx = listenerChunk.pos.x;
            int ly = listenerChunk.pos.y;
            int lz = listenerChunk.pos.z;

            if (ly != cy && lz != cz)
                return null;

            if ((pos.x!=0) || (lx+m_sideSize!=cx))
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return listenerChunk.blocks;
        }

        private ChunkBlocks HandleNeighborUp(ref Vector3Int pos)
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            int i = DirectionUtils.Get(Direction.up);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var listeners = stateManager.Listeners;
            ChunkEvent listener = listeners[i];
            if (listener == null)
                return null;

            ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
            Chunk listenerChunk = listenerClient.chunk;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;
            int lx = listenerChunk.pos.x;
            int ly = listenerChunk.pos.y;
            int lz = listenerChunk.pos.z;

            if (lx!=cx && lz!=cz)
                return null;

            if ((pos.y!=(m_sideSize-1)) || (ly-m_sideSize!=cy))
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return listenerChunk.blocks;
        }

        private ChunkBlocks HandleNeighborDown(ref Vector3Int pos)
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            int i = DirectionUtils.Get(Direction.down);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var listeners = stateManager.Listeners;
            ChunkEvent listener = listeners[i];
            if (listener == null)
                return null;

            ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
            Chunk listenerChunk = listenerClient.chunk;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;
            int lx = listenerChunk.pos.x;
            int ly = listenerChunk.pos.y;
            int lz = listenerChunk.pos.z;

            if (lx != cx && lz != cz)
                return null;

            if ((pos.y!=0) || (ly+m_sideSize!=cy))
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return listenerChunk.blocks;
        }

        private ChunkBlocks HandleNeighborFront(ref Vector3Int pos)
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            int i = DirectionUtils.Get(Direction.north);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var listeners = stateManager.Listeners;
            ChunkEvent listener = listeners[i];
            if (listener == null)
                return null;
            
            ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
            Chunk listenerChunk = listenerClient.chunk;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;
            int lx = listenerChunk.pos.x;
            int ly = listenerChunk.pos.y;
            int lz = listenerChunk.pos.z;

            if (ly!=cy && lx!=cx)
                return null;

            if ((pos.z!=(m_sideSize-1)) || (lz-m_sideSize!=cz))
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return listenerChunk.blocks;
        }

        private ChunkBlocks HandleNeighborBack(ref Vector3Int pos)
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            int i = DirectionUtils.Get(Direction.south);

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild
            var listeners = stateManager.Listeners;
            ChunkEvent listener = listeners[i];
            if (listener == null)
                return null;
            
            ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
            Chunk listenerChunk = listenerClient.chunk;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;
            int lx = listenerChunk.pos.x;
            int ly = listenerChunk.pos.y;
            int lz = listenerChunk.pos.z;

            if (ly != cy && lx != cx)
                return null;

            if (pos.z!=0 || (lz+m_sideSize!=cz))
                return null;

            rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);
            return listenerChunk.blocks;
        }

        public ChunkBlocks HandleNeighbor(ref Vector3Int pos, Direction dir)
        {
            switch (dir)
            {
                case Direction.up:
                    return HandleNeighborUp(ref pos);
                case Direction.down:
                    return HandleNeighborDown(ref pos);
                case Direction.north:
                    return HandleNeighborFront(ref pos);
                case Direction.south:
                    return HandleNeighborBack(ref pos);
                case Direction.east:
                    return HandleNeighborRight(ref pos);
                default: //Direction.west
                    return HandleNeighborLeft(ref pos);
            }
        }

        public void HandleNeighbors(BlockData block, Vector3Int pos)
        {
            if (!NeedToHandleNeighbors(ref pos))
                return;

            int cx = chunk.pos.x;
            int cy = chunk.pos.y;
            int cz = chunk.pos.z;

            ChunkStateManagerClient stateManager = chunk.stateManager;

            // If it is an edge position, notify neighbor as well
            // Iterate over neighbors and decide which ones should be notified to rebuild their geometry
            var listeners = stateManager.Listeners;
            for (int i = 0; i < listeners.Length; i++)
            {
                ChunkEvent listener = listeners[i];
                if (listener == null)
                    continue;

                ChunkStateManagerClient listenerClient = (ChunkStateManagerClient)listener;
                Chunk listenerChunk = listenerClient.chunk;
                ChunkBlocks listenerChunkBlocks = listenerChunk.blocks;

                int lx = listenerChunk.pos.x;
                int ly = listenerChunk.pos.y;
                int lz = listenerChunk.pos.z;

                if (ly == cy || lz == cz)
                {
                    // Section to the left
                    if ((pos.x == 0) && (lx + m_sideSize == cx))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(m_sideSize, pos.y, pos.z, m_pow);
                        listenerChunkBlocks.blocks[neighborIndex] = block;
                    }
                    // Section to the right
                    else if ((pos.x == (m_sideSize-1)) && (lx - m_sideSize == cx))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(-1, pos.y, pos.z, m_pow);
                        listenerChunkBlocks.blocks[neighborIndex] = block;
                    }
                }

                if (lx == cx || lz == cz)
                {
                    // Section to the bottom
                    if ((pos.y == 0) && (ly + m_sideSize == cy))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, m_sideSize, pos.z, m_pow);
                        listenerChunkBlocks.blocks[neighborIndex] = block;
                    }
                    // Section to the top
                    else if ((pos.y == (m_sideSize-1)) && (ly - m_sideSize == cy))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, -1, pos.z, m_pow);
                        listenerChunkBlocks.blocks[neighborIndex] = block;
                    }
                }

                if (ly == cy || lx == cx)
                {
                    // Section to the back
                    if ((pos.z == 0) && (lz + m_sideSize == cz))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, m_sideSize, m_pow);
                        listenerChunkBlocks.blocks[neighborIndex] = block;
                    }
                    // Section to the front
                    else if ((pos.z == (m_sideSize-1)) && (lz - m_sideSize == cz))
                    {
                        rebuildMaskGeometry = rebuildMaskGeometry | (1 << i);

                        // Mirror the block to the neighbor edge
                        int neighborIndex = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, -1, m_pow);
                        listenerChunkBlocks.blocks[neighborIndex] = block;
                    }
                }

                // No further checks needed once we know all neighbors need to be notified
                if (rebuildMaskGeometry == 0x3f)
                    break;
            }
        }

        public void ProcessSetBlock(BlockData block, int index, bool setBlockModified)
        {
            int x, y, z;
            Helpers.GetChunkIndex3DFrom1D(index, out x, out y, out z, m_pow);
            Vector3Int pos = new Vector3Int(x, y, z);
            
            BlockData oldBlockData = blocks[index];
            Block oldBlock = m_blockTypes[oldBlockData.Type];
            Block newBlock = m_blockTypes[block.Type];

            oldBlock.OnDestroy(chunk, ref pos);
            newBlock.OnCreate(chunk, ref pos);

            SetInner(index, block);

            if (setBlockModified)
            {
                Vector3Int globalPos = pos+chunk.pos;
                BlockModified(new BlockPos(pos.x, pos.y, pos.z), ref globalPos, block);
            }
        }

        public void Update()
        {
            ChunkStateManagerClient stateManager = chunk.stateManager;
            if (!stateManager.IsUpdateBlocksPossible)
                return;

            //UnityEngine.Debug.Log(m_setBlockQueue.Count);

            if (m_setBlockQueue.Count>0)
            {
                if (rebuildMaskGeometry<0)
                    rebuildMaskGeometry = 0;
                if (rebuildMaskCollider<0)
                    rebuildMaskCollider = 0;

                var timeBudget = Globals.SetBlockBudget;
                
                // Modify blocks
                int j;
                for (j = 0; j<m_setBlockQueue.Count; j++)
                {
                    timeBudget.StartMeasurement();
                    m_setBlockQueue[j].Apply(this);
                    timeBudget.StopMeasurement();

                    // Sync edges if there's enough time
                    /*if (!timeBudget.HasTimeBudget)
                    {
                        ++j;
                        break;
                    }*/
                }

                rebuildMaskCollider |= rebuildMaskGeometry;

                if (j==m_setBlockQueue.Count)
                    m_setBlockQueue.Clear();
                else
                {
                    m_setBlockQueue.RemoveRange(0, j);
                    return;
                }
            }

            long now = Globals.Watch.ElapsedMilliseconds;

            // Request a geometry update at most 10 times a second
            if (rebuildMaskGeometry>=0 && now-lastUpdateTimeGeometry>=100)
            {
                lastUpdateTimeGeometry = now;

                // Request rebuild on this chunk
                stateManager.RequestState(ChunkState.BuildVerticesNow);

                // Notify neighbors that they need to rebuild their geometry
                if (rebuildMaskGeometry>0)
                {
                    var listeners = stateManager.Listeners;
                    for (int j = 0; j<listeners.Length; j++)
                    {
                        ChunkStateManagerClient listener = (ChunkStateManagerClient)listeners[j];
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
                    var listeners = stateManager.Listeners;
                    for (int j = 0; j < listeners.Length; j++)
                    {
                        ChunkStateManagerClient listener = (ChunkStateManagerClient)listeners[j];
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
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z, m_pow);
            return blocks[index];
        }

        /// <summary>
        /// Returns a block from a position within the chunk
        /// </summary>
        /// <param name="pos">Position in local chunk coordinates</param>
        /// <returns>The block at the position</returns>
        public Block GetBlock(ref Vector3Int pos)
        {
            int index = Helpers.GetChunkIndex1DFrom3D(pos.x, pos.y, pos.z, m_pow);
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
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int index = Helpers.GetChunkIndex1DFrom3D(posFrom.x, posFrom.y, posFrom.z, m_pow);
            int yOffset = sizeWithPaddingPow2-(posTo.z-posFrom.z+1)*sizeWithPadding;
            int zOffset = sizeWithPadding-(posTo.x-posFrom.x+1);

            for (int y = posFrom.y; y<=posTo.y; ++y, index+=yOffset)
            {
                for (int z = posFrom.z; z<=posTo.z; ++z, index+=zOffset)
                {
                    for (int x = posFrom.x; x <= posTo.x; ++x, ++index)
                    {
                        SetInner(index, blockData);
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
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int index = Helpers.GetChunkIndex1DFrom3D(posFrom.x, posFrom.y, posFrom.z, m_pow);
            int yOffset = sizeWithPaddingPow2-(posTo.z-posFrom.z+1)*sizeWithPadding;
            int zOffset = sizeWithPadding-(posTo.x-posFrom.x+1);

            for (int y = posFrom.y; y<=posTo.y; ++y, index+=yOffset)
            {
                for (int z = posFrom.z; z<=posTo.z; ++z, index+=zOffset)
                {
                    for (int x = posFrom.x; x <= posTo.x; ++x, ++index)
                    {
                        SetRaw(index, blockData);
                    }
                }
            }
        }
        
        /// <summary>
        /// Queues a modification of blocks in a given range
        /// </summary>
        /// <param name="op">Set operation to be performed</param>
        public void Modify(ModifyOp op)
        {
            m_setBlockQueue.Add(op);
        }

        public void BlockModified(BlockPos blockPos, ref Vector3Int globalPos, BlockData blockData)
        {
            VmNetworking ntw = chunk.world.networking;

            // If this is the server log the changed block so that it can be saved
            if (ntw.isServer)
            {
                if (ntw.allowConnections)
                    ntw.server.BroadcastChange(globalPos, blockData, -1);

                if (Features.UseDifferentialSerialization)
                {
                    // TODO: Memory unfriendly. Rethink the strategy
                    modifiedBlocks.Add(blockPos);
                }
            }
            else // if this is not the server send the change to the server to sync
            {
                ntw.client.BroadcastChange(globalPos, blockData);
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
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int yOffset = sizeWithPaddingPow2 - (z2 - z1) * sizeWithPadding;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x2, y1, z1, m_pow);

            // Check the quad formed by YZ axes and try to expand the X asix
            int index = index0;
            for (int y = y1; y<y2; ++y, index+=yOffset)
            {
                for (int z = z1; z<z2; ++z, index += sizeWithPadding)
                {
                    if (mask[index] || blocks[index].Type!=type)
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

        private bool ExpandY(ref bool[] mask, ushort type, int x1, int z1, int x2, ref int y2, int z2)
        {
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;

            int zOffset = sizeWithPadding - x2 + x1;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x1, y2, z1, m_pow);

            // Check the quad formed by XZ axes and try to expand the Y axis
            int index = index0;
            for (int z = z1; z<z2; ++z, index+=zOffset)
            {
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || blocks[index].Type!=type)
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

        private bool ExpandZ(ref bool[] mask, ushort type, int x1, int y1, int x2, int y2, ref int z2)
        {
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            int yOffset = sizeWithPaddingPow2 - x2 + x1;
            int index0 = Helpers.GetChunkIndex1DFrom3D(x1, y1, z2, m_pow);

            // Check the quad formed by XY axes and try to expand the Z axis
            int index = index0;
            for (int y = y1; y<y2; ++y, index+=yOffset)
            {
                for (int x = x1; x<x2; ++x, ++index)
                {
                    if (mask[index] || blocks[index].Type!=type)
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

        /// <summary>
        /// Compresses chunk's memory.
        /// </summary>
        public void Compress()
        {
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;
            int sizeWithPaddingPow3 = sizeWithPaddingPow2 * sizeWithPadding;
            int sizePlusPadding = m_sideSize + Env.ChunkPadding;

            var pools = chunk.pools;
            bool[] mask = pools.BoolArrayPool.PopExact(sizeWithPaddingPow3);

            Array.Clear(mask, 0, mask.Length);
            blockCompressed.Clear();

            // This compression is essentialy RLE. However, instead of working on 1 axis
            // it works in 3 dimensions.
            int index = 0;
            for (int y = -1; y<sizePlusPadding; ++y, index+=sizeWithPaddingPow2)
            {
                for (int z = -1; z<sizePlusPadding; ++z, index+=sizeWithPadding)
                {
                    for (int x = -1; x<sizePlusPadding; ++x, ++index)
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
                            
                            if (expandY)
                            {
                                expandY = y2<sizePlusPadding &&
                                          ExpandY(ref mask, type, x1, z1, x2, ref y2, z2);
                                expand = expandY;
                            }
                            if (expandZ)
                            {
                                expandZ = z2<sizePlusPadding &&
                                          ExpandZ(ref mask, type, x1, y1, x2, y2, ref z2);
                                expand = expand|expandZ;
                            }
                            if (expandX)
                            {
                                expandX = x2 < sizePlusPadding &&
                                          ExpandX(ref mask, type, y1, z1, ref x2, y2, z2);
                                expand = expand|expandX;
                            }
                        } while (expand);

                        blockCompressed.Add(new BlockDataAABB(data, x1, y1, z1, x2, y2, z2));

                        /*// Let's make sure that we don't take too much space
                        int compressedSize = blockCompressed.Count*StructSerialization.TSSize<BlockDataAABB>.ValueSize;
                        int decompressedSize = sizeWithPaddingPow3*StructSerialization.TSSize<BlockData>.ValueSize;
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
            int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
            int sizeWithPaddingPow2 = sizeWithPadding * sizeWithPadding;

            for (int i = 0; i<blockCompressed.Count; i++)
            {
                var box = blockCompressed[i];
                int x1 = box.MinX;
                int y1 = box.MinY;
                int z1 = box.MinZ;
                int x2 = box.MaxX;
                int y2 = box.MaxY;
                int z2 = box.MaxZ;
                ushort data = box.Data;

                int index = Helpers.GetChunkIndex1DFrom3D(x1, y1, z1, m_pow);
                int yOffset = sizeWithPaddingPow2-(z2-z1)*sizeWithPadding;
                int zOffset = sizeWithPadding-(x2-x1);
                for (int y = y1; y<y2; ++y, index+=yOffset)
                {
                    for (int z = z1; z<z2; ++z, index+=zOffset)
                    {
                        for (int x = x1; x<x2; ++x, ++index)
                        {
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
                
                int sizeWithPadding = m_sideSize + Env.ChunkPadding2;
                int sizeWithPaddingPow3 = sizeWithPadding * sizeWithPadding * sizeWithPadding;

                for (int index = 1; index < sizeWithPaddingPow3; ++index)
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