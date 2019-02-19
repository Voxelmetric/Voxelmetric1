#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
using System;
using UnityEngine;
using Voxelmetric.Code.Common;

namespace Voxelmetric.Code.Utilities.Noise
{
    public static class NoiseUtilsSIMD
    {
        public static float GetNoise(float[] noiseSet, int sideSize, int x, int z, int max, float power)
        {
            // FastSIMD keeps things in x,z fashion but we have them ordered as z,x
            int index = Helpers.GetIndex1DFrom2D(z, x, sideSize);
            float n = noiseSet[index]+1f;
            n *= (max>>1);

            if (Math.Abs(power-1f)>float.Epsilon)
                n = Mathf.Pow(n, power);

            return n;
        }

        public static float GetNoise(float[] noiseSet, int sideSize, int x, int y, int z, int max, float power)
        {
            // FastSIMD keeps things in x,y,z fashion but we have them ordered as y,z,x
            int index = Helpers.GetIndex1DFrom3D(z, x, y, sideSize, sideSize);
            float n = noiseSet[index]+1f;
            n *= (max>>1);

            if (Math.Abs(power-1f)>float.Epsilon)
                n = Mathf.Pow(n, power);

            return n;
        }
    }
}

#endif