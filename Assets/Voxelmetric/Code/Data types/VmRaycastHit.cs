using UnityEngine;
using Voxelmetric.Code.Core;

namespace Voxelmetric.Code.Data_types
{
    public struct VmRaycastHit
    {
        public Vector3Int vector3Int;
        public Vector3Int adjacentPos;
        public Vector3 dir;
        public float distance;
        public Block block;
        public World world;
        public Vector3 scenePos;
    }
}
