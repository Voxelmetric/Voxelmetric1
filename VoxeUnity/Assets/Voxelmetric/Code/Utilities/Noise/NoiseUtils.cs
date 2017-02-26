using System;
using UnityEngine;

namespace Voxelmetric.Code.Utilities.Noise
{
    public static class NoiseUtils
    {
        public static float GetNoise(FastNoise noise, float x, float y, float scale, int max, float power)
        {
            float scaleInv = 1f/scale;
            float n = noise.GetSimplex(x*scaleInv, y*scaleInv)+1f;
            n *= (max>>1);

            if (Math.Abs(power-1f)>float.Epsilon)
                n = Mathf.Pow(n, power);

            return n;
        }

        public static float GetNoise(FastNoise noise, float x, float y, float z, float scale, int max, float power)
        {
            float scaleInv = 1f/scale;
            float n = noise.GetSimplex(x*scaleInv, y*scaleInv, z*scaleInv)+1f;
            n *= (max>>1);

            if (Math.Abs(power-1f)>float.Epsilon)
                n = Mathf.Pow(n, power);

            return n;
        }
    }
}