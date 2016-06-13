using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code
{
    public static class Voxelmetric
    {
        //Used as a manager class with references to classes treated like singletons
        public static VoxelmetricResources resources = new VoxelmetricResources ();

        public static void SetBlock(World world, BlockPos pos, BlockData blockData)
        {
            world.blocks.Modify(pos, blockData);
        }

        public static Block GetBlock(World world, BlockPos pos)
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
                if (!chunk.IsSavePossible)
                    continue;

                chunksToSave.Add(chunk);
                chunk.RequestSaveData();
            }

            return chunksToSave;
        }

        public static VmRaycastHit Raycast(Ray ray, World world, float range = 10000f)
        {
            return VmRaycast.Raycast(ray, world, range);
        }
    }
}