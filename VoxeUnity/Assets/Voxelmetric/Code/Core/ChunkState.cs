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
        PrepareSaveData = 0x08,
        SaveData = 0x10, //! Chunk stores its data
        BuildCollider = 0x20, //! Chunk generates its render geometry
        BuildVertices = 0x40, //! Chunk generates its collision geometry
        BuildVerticesNow = 0x80, //! Chunk generates its collision geometry with priority
        Remove = 0x100, //! Chunk is waiting for removal
    }
}
