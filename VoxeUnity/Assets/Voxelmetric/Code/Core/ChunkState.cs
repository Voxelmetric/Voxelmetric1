using System;

namespace Voxelmetric.Code.Core
{
    [Flags]
    public enum ChunkState : ushort
    {
        Idle = 0,

        LoadData = 0x01, //! Chunk loads its data
        Generate = 0x02,  //! Chunk is generated
        CalculateBounds = 0x04, //! Chunk calculatse its bounds
        SaveData = 0x08, //! Chunk stores its data
        BuildCollider = 0x10, //! Chunk generates its render geometry
        BuildVertices = 0x20, //! Chunk generates its collision geometry
        BuildVerticesNow = 0x40, //! Chunk generates its collision geometry with priority        
        Remove = 0x80, //! Chunk is waiting for removal
    }
}
