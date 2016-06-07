using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Data_types
{
    public struct VmRaycastHit
    {
        public BlockPos blockPos;
        public BlockPos adjacentPos;
        public Vector3 dir;
        public float distance;
        public Block block;
        public World world;
        public Vector3 scenePos;
    }
}
