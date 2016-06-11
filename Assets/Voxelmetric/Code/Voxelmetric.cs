using System.Collections.Generic;
using UnityEngine;
using Voxelmetric.Code.Core;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Serialization;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code
{
    public static class Voxelmetric
    {
        //Used as a manager class with references to classes treated like singletons
        public static VoxelmetricResources resources = new VoxelmetricResources ();
        
        public static bool SetBlock (BlockPos pos, Block block, World world)
        {
            Chunk chunk = world.chunks.Get (pos);
            if (chunk == null)
                return false;

            chunk.world.blocks.Set (pos, block);

            return true;
        }

        public static Block GetBlock (BlockPos pos, World world)
        {
            Block block = world.blocks.Get (pos);

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