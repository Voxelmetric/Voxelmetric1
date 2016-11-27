using System.Collections.Generic;
using System.Linq;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public static class ChunkLoadOrder
    {
        public static Vector3Int[] ChunkPositions(int chunkLoadRadius)
        {
            var chunkLoads = new List<Vector3Int>();
            for (int x = -chunkLoadRadius; x <= chunkLoadRadius; x++)
            {
                for (int z = -chunkLoadRadius; z <= chunkLoadRadius; z++)
                {
                    chunkLoads.Add(new Vector3Int(x, 0, z));
                }
            }

            // limit how far away the blocks can be to achieve a circular loading pattern
            float maxRadius = chunkLoadRadius * 1.55f;

            //sort 2d vectors by closeness to center
            return chunkLoads
                .Where(pos => Helpers.Abs(pos.x) + Helpers.Abs(pos.z) < maxRadius)
                .OrderBy(pos => Helpers.Abs(pos.x) + Helpers.Abs(pos.z)) //smallest magnitude vectors first
                .ThenBy(pos => Helpers.Abs(pos.x)) //make sure not to process e.g (-10,0) before (5,5)
                .ThenBy(pos => Helpers.Abs(pos.z))
                .ToArray();
        }

    }
}