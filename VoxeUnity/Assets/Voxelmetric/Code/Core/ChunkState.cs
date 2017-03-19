using System;

namespace Voxelmetric.Code.Core
{
    [Flags]
    public enum ChunkState : ushort
    {
        Idle = 0,

        LoadData = 0x01, //! Chunk loads its data
        PrepareGenerate = 0x02,
        Generate = 0x04,  //! Chunk is generated
        CalculateBounds = 0x08, //! Chunk calculatse its bounds
        PrepareSaveData = 0x10,
        SaveData = 0x20, //! Chunk stores its data
        BuildCollider = 0x40, //! Chunk generates its render geometry
        BuildVertices = 0x80, //! Chunk generates its collision geometry
        BuildVerticesNow = 0x100, //! Chunk generates its collision geometry with priority        
        Remove = 0x200, //! Chunk is waiting for removal
    }
}
