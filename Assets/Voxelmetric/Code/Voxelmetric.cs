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
        /// Saves all chunks currently loaded, if UseMultiThreading is enabled it saves the chunks
        ///  asynchronously and the SaveProgress object returned will show the progress
        /// </summary>
        /// <param name="world">Optional parameter for the world to save chunks for, if left
        /// empty it will use the world Singleton instead</param>
        /// <returns>A SaveProgress object to monitor the save.</returns>
        public static SaveProgress SaveAll (World world)
        {
            //Create a saveprogress object with positions of all the chunks in the world
            //Then save each chunk and update the saveprogress's percentage for each save
            SaveProgress saveProgress = new SaveProgress (world.chunks.posCollection);
            List<Chunk> chunksToSave = new List<Chunk> ();
            chunksToSave.AddRange (world.chunks.chunkCollection);

            foreach (var chunk in chunksToSave)
            {
                chunk.RequestSaveData();

                // TODO! Make saveProgress work with the new system
                /*if (!Serialization.SaveChunk(chunk))
                    saveProgress.SaveErrorForChunk(chunk.pos);
                else
                    saveProgress.SaveCompleteForChunk(chunk.pos);*/
            }

            return saveProgress;
        }

        public static VmRaycastHit Raycast(Ray ray, World world, float range = 10000f)
        {
            return VmRaycast.Raycast(ray, world, range);
        }
    }
}