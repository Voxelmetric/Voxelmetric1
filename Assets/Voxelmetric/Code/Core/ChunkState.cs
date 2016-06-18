using System;

namespace Voxelmetric.Code.Core
{
    [Flags]
    public enum ChunkState : byte
    {
        Idle = 0,

        Generate = 0x01,  //! Chunk is generated
        LoadData = 0x02, //! Chunk loads its data
        GenericWork = 0x04, //! Some generic work
        BuildVertices = 0x08, //! Chunk is building its vertex data
        BuildVerticesNow = 0x10,
        SaveData = 0x20, //! Chunk stores its data
        Remove = 0x40, //! Chunk is waiting for removal
    }
}
