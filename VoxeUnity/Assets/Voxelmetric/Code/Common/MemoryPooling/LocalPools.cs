using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Data_types;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities;
using Voxelmetric.Code.Utilities.Noise;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools
    {
        public NoiseItem[] noiseItems;
        
        public readonly ObjectPool<VertexData> VertexDataPool =
            new ObjectPool<VertexData>(ch => new VertexData(), 65535, false);

        public readonly ArrayPoolCollection<VertexData> VertexDataArrayPool =
            new ArrayPoolCollection<VertexData>(128);

        public readonly ArrayPoolCollection<VertexDataFixed> VertexDataFixedArrayPool =
            new ArrayPoolCollection<VertexDataFixed>(128);

        public readonly ArrayPoolCollection<Vector3> Vector3ArrayPool =
            new ArrayPoolCollection<Vector3>(128);

        public readonly ArrayPoolCollection<bool> BoolArrayPool =
            new ArrayPoolCollection<bool>(128);

        public readonly ArrayPoolCollection<byte> ByteArrayPool =
            new ArrayPoolCollection<byte>(128);

        public readonly ArrayPoolCollection<float> FloatArrayPool =
            new ArrayPoolCollection<float>(128);

        public readonly ArrayPoolCollection<BlockFace> BlockFaceArrayPool =
            new ArrayPoolCollection<BlockFace>(128);

        public readonly MarshalMemPool MarshaledPool =
            new MarshalMemPool(Env.ChunkSizeWithPaddingPow3*8); // Set to a multiple of chunk volume
        
        public override string ToString()
        {
            StringBuilder sb = new StringBuilder();
            sb.Append("VertexData=");
            sb.Append(VertexDataPool);
            sb.Append(",VertexDataArray=");
            sb.Append(VertexDataArrayPool);
            sb.Append(",VertexDataFixed=");
            sb.Append(VertexDataFixedArrayPool);
            sb.Append(",Vec3Arr=");
            sb.Append(Vector3ArrayPool);
            sb.Append(",BoolArr=");
            sb.Append(BoolArrayPool);
            sb.Append(",FloatArr=");
            sb.Append(FloatArrayPool);
            sb.Append(",MarshaledBLeft=");
            sb.Append(MarshaledPool);
            return sb.ToString();
        }
    }
}
