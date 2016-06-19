using System.IO;

namespace Voxelmetric.Code.Common.IO
{
    public interface IBinarizable
    {
        void Binarize(BinaryWriter bw);
        void Debinarize(BinaryReader br);
    }
}
