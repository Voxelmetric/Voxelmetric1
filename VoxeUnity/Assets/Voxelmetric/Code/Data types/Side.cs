using System;

namespace Voxelmetric.Code.Data_types
{
    [Flags]
    public enum Side: byte
    {
        up = 0x01, // front side
        down = 0x02, // back side

        north = 0x04, // front side
        south = 0x08, // back side

        east = 0x10, // front side
        west = 0x20 // back side
    }
}
