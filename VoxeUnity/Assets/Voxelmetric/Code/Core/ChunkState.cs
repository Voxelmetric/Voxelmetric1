using System;

namespace Voxelmetric.Code.Core
{
    [Flags]
    public enum ChunkState : ushort
    {
        Idle = 0,

        Generate = 0x01,  //! Chunk is generated
        LoadData = 0x02, //! Chunk loads its data
        GenericWork = 0x04, //! Some generic work
        BuildCollider = 0x08,
        BuildVertices = 0x10, //! Chunk is building its vertex data
        BuildVerticesNow = 0x20,
        SaveData = 0x40, //! Chunk stores its data
        Remove = 0x80, //! Chunk is waiting for removal
    }
}
