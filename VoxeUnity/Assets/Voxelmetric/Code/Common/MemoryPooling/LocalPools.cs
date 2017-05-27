using System.Text;
using UnityEngine;
using Voxelmetric.Code.Common.Extensions;
using Voxelmetric.Code.Common.Memory;
using Voxelmetric.Code.Configurable.Blocks;
using Voxelmetric.Code.Rendering;
using Voxelmetric.Code.Utilities.Noise;

namespace Voxelmetric.Code.Common.MemoryPooling
{
    /// <summary>
    ///     Local object pools for often used heap objects.
    /// </summary>
    public class LocalPools
    {
        public NoiseItem[] noiseItems;
        
        public readonly ArrayPoolCollection<VertexData> VertexDataArrayPool =
            new ArrayPoolCollection<VertexData>(128);

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
            StringBuilder sb = new StringBuilder(256);
            sb.ConcatFormat("VertexData={0}", VertexDataArrayPool.ToString());
            sb.ConcatFormat(",Vec3Arr={0}", Vector3ArrayPool.ToString());
            sb.ConcatFormat(",BoolArr={0}", BoolArrayPool.ToString());
            sb.ConcatFormat(",ByteArr={0}", ByteArrayPool.ToString());
            sb.ConcatFormat(",FloatArr={0}", FloatArrayPool.ToString());
            sb.ConcatFormat(",BlockFaceArr={0}", BlockFaceArrayPool.ToString());
            sb.ConcatFormat(",MarshaledBLeft={0}", MarshaledPool.ToString());
            return sb.ToString();
        }
    }
}
