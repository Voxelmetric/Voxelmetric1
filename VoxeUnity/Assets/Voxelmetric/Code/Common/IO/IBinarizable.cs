using System.IO;

namespace Voxelmetric.Code.Common.IO
{
    public interface IBinarizable
    {
        bool Binarize(BinaryWriter bw);
        bool Debinarize(BinaryReader br);
    }
}
