using System;

namespace Voxelmetric.Code.Core
{
    [Flags]
    public enum ChunkState : ushort
    {
        Idle = 0,

        Generate = 0x01,  //! Chunk is generated
        LoadData = 0x02, //! Chunk loads its data
        CalculateBounds = 0x04, //! Chunk calculatse its bounds
        BuildCollider = 0x08, //! Chunk generates its render geometry
        BuildVertices = 0x10, //! Chunk generates its collision geometry
        BuildVerticesNow = 0x20, //! Chunk generates its collision geometry with priority
        SaveData = 0x40, //! Chunk stores its data
        Remove = 0x80, //! Chunk is waiting for removal
    }
}
