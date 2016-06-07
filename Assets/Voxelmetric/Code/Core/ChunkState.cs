using System;

namespace Voxelmetric.Code.Core
{
    [Flags]
    public enum ChunkState : byte
    {
        Idle = 0,

        Generate = 0x01,  //! Chunk is generated
        LoadData = 0x02, //! Chunk loads its data
        BuildVertices = 0x04, //! Chunk is building its vertex data
        SaveData = 0x08, //! Chunk stores its data
        Remove = 0x10, //! Chunk is waiting for removal

        GenericWork = 0x20 //! Some generic work
    }
}
