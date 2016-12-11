using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Core.StateManager;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code
{
    public static class Voxelmetric
    {
        //Used as a manager class with references to classes treated like singletons
        public static readonly VoxelmetricResources resources = new VoxelmetricResources ();

        public static void SetBlock(World world, Vector3Int pos, BlockData blockData)
        {
            world.blocks.Modify(pos, blockData, true);
        }

        public static void SetBlockRange(World world, Vector3Int posFrom, Vector3Int posTo, BlockData blockData)
        {
            world.blocks.ModifyRange(posFrom, posTo, blockData, true);
        }

        public static Block GetBlock(World world, Vector3Int pos)
        {
            Block block = world.blocks.GetBlock(pos);
            return block;
        }

        /// <summary>
        /// Sends a save request to all chunk currently loaded
        /// </summary>
        /// <param name="world">World holding chunks</param>
        /// <returns>List of chunks waiting to be saved.</returns>
        public static List<Chunk> SaveAll (World world)
        {
            if (world==null)
                return null;

            List<Chunk> chunksToSave = new List<Chunk> ();

            foreach (Chunk chunk in world.chunks.chunkCollection)
            {
                // Ignore chunks that can't be saved at the moment
                ChunkStateManagerClient stateManager = (ChunkStateManagerClient)chunk.stateManager;
                if (!stateManager.IsSavePossible)
                    continue;

                chunksToSave.Add(chunk);
                stateManager.RequestState(ChunkState.SaveData);
            }

            return chunksToSave;
        }

        public static VmRaycastHit Raycast(Ray ray, World world, float range, bool removalRequested)
        {
            return VmRaycast.Raycast(ray, world, range, removalRequested);
        }
    }
}