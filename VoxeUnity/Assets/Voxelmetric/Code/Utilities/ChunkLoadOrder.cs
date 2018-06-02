using System.Collections.Generic;
using System.Linq;
using Voxelmetric.Code.Common;
using Voxelmetric.Code.Data_types;

namespace Voxelmetric.Code.Utilities
{
    public static class ChunkLoadOrder
    {
        private static List<Vector3Int> s_chunkLoads = new List<Vector3Int>();

        public static Vector3Int[] ChunkPositions(int chunkLoadRadius)
        {
            s_chunkLoads.Clear();
            
            for (int z = -chunkLoadRadius; z <= chunkLoadRadius; z++)
            {
                for (int x = -chunkLoadRadius; x <= chunkLoadRadius; x++)
                {
                    s_chunkLoads.Add(new Vector3Int(x, 0, z));
                }
            }

            // Sort 2D vectors by closeness to the center
            return s_chunkLoads
                .Where(pos => CheckXZ(pos.x, pos.z, chunkLoadRadius))
                // Smallest magnitude vectors first
                .OrderBy(pos => Helpers.Abs(pos.x) + Helpers.Abs(pos.z))
                // Make sure not to process e.g (-10,0) before (5,5)
                .ThenBy(pos => Helpers.Abs(pos.x)) 
                .ThenBy(pos => Helpers.Abs(pos.z))
                .ToArray();
        }

        public static bool CheckXZ(int x, int z, int dist)
        {
            return x * x + z * z <= dist * dist; // circle
        }

        public static bool CheckY(int y, int dist)
        {
            return Helpers.Abs(y) <= dist; // square
        }
    }
}