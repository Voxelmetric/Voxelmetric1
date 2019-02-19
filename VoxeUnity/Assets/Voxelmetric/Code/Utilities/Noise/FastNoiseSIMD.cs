#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
using System.Runtime.InteropServices;
#if UNITY_64|| UNITY_EDITOR64
using POINTER = System.UInt64;
#else
using POINTER = System.UInt32;
#endif

namespace Voxelmetric.Code.Utilities.Noise
{
    public class FastNoiseSIMD
    {
        public enum NoiseType
        {
            Value,
            ValueFractal,
            Perlin,
            PerlinFractal,
            Simplex,
            SimplexFractal,
            WhiteNoise,
            Cellular
        };

        public enum FractalType
        {
            FBM,
            Billow,
            RigidMulti
        };

        public enum CellularDistanceFunction
        {
            Euclidean,
            Manhattan,
            Natural
        };

        public enum CellularReturnType
        {
            CellValue,
            Distance,
            Distance2,
            Distance2Add,
            Distance2Sub,
            Distance2Mul,
            Distance2Div
        };

        private readonly POINTER noisePointer;

        public FastNoiseSIMD(int seed = 1337)
        {
            noisePointer = CreateInstance(seed);
        }

        ~FastNoiseSIMD()
        {
            ReleaseInstance(noisePointer);
        }

        public int GetSeed()
        {
            return GetSeed(noisePointer);
        }

        public void SetSeed(int seed)
        {
            SetSeed(noisePointer, seed);
        }

        public void SetFrequency(float frequency)
        {
            SetFrequency(noisePointer, frequency);
        }

        public void SetNoiseType(NoiseType noiseType)
        {
            SetNoiseType(noisePointer, (int)noiseType);
        }

        public void SetAxisScales(float xScale, float yScale, float zScale)
        {
            SetAxisScales(noisePointer, xScale, yScale, zScale);
        }

        public void SetFractalOctaves(int octaves)
        {
            SetFractalOctaves(noisePointer, octaves);
        }

        public void SetFractalLacunarity(float lacunarity)
        {
            SetFractalLacunarity(noisePointer, lacunarity);
        }

        public void SetFractalGain(float gain)
        {
            SetFractalGain(noisePointer, gain);
        }

        public void SetFractalType(FractalType fractalType)
        {
            SetFractalType(noisePointer, (int)fractalType);
        }

        public void SetCellularReturnType(CellularReturnType cellularReturnType)
        {
            SetCellularReturnType(noisePointer, (int)cellularReturnType);
        }

        public void SetCellularDistanceFunction(CellularDistanceFunction cellularDistanceFunction)
        {
            SetCellularDistanceFunction(noisePointer, (int)cellularDistanceFunction);
        }

        public void FillNoiseSet(float[] noiseSet, int xStart, int yStart, int zStart, int xSize, int ySize, int zSize,
            float scaleModifier = 1.0f)
        {
            FillNoiseSet(noisePointer, noiseSet, xStart, yStart, zStart, xSize, ySize, zSize, scaleModifier);
        }

        public void FillSampledNoiseSet(float[] noiseSet, int xStart, int yStart, int zStart, int xSize, int ySize,
            int zSize, int sampleScale)
        {
            FillSampledNoiseSet(noisePointer, noiseSet, xStart, yStart, zStart, xSize, ySize, zSize, sampleScale);
        }

#if UNITY_64 || UNITY_EDITOR64
        private const string DLL_NAME = "FastNoise_x64";
#else
        private const string DLL_NAME = "FastNoise";
#endif

        [DllImport(DLL_NAME)]
        public static extern POINTER CreateInstance(int seed);

        [DllImport(DLL_NAME)]
        public static extern void ReleaseInstance(POINTER noisePointer);

        [DllImport(DLL_NAME)]
        private static extern void SetSeed(POINTER noisePointer, int seed);

        [DllImport(DLL_NAME)]
        private static extern int GetSeed(POINTER noisePointer);

        [DllImport(DLL_NAME)]
        private static extern void SetNoiseType(POINTER noisePointer, int noiseType);

        [DllImport(DLL_NAME)]
        private static extern void SetFractalType(POINTER noisePointer, int fractalType);

        [DllImport(DLL_NAME)]
        private static extern void SetFrequency(POINTER noisePointer, float freq);

        [DllImport(DLL_NAME)]
        private static extern void SetFractalOctaves(POINTER noisePointer, int octaves);

        [DllImport(DLL_NAME)]
        private static extern void SetFractalLacunarity(POINTER noisePointer, float lacunarity);

        [DllImport(DLL_NAME)]
        private static extern void SetFractalGain(POINTER noisePointer, float gain);

        [DllImport(DLL_NAME)]
        private static extern void SetAxisScales(POINTER noisePointer, float xScale, float yScale, float zScale);

        [DllImport(DLL_NAME)]
        private static extern void SetCellularDistanceFunction(POINTER noisePointer, int distanceFunction);

        [DllImport(DLL_NAME)]
        private static extern void SetCellularReturnType(POINTER noisePointer, int returnType);

        [DllImport(DLL_NAME)]
        private static extern void FillNoiseSet(POINTER noisePointer, float[] noiseSet, int xStart, int yStart,
            int zStart, int xSize, int ySize, int zSize, float scaleModifier);

        [DllImport(DLL_NAME)]
        private static extern void FillSampledNoiseSet(POINTER noisePointer, float[] noiseSet, int xStart,
            int yStart, int zStart, int xSize, int ySize, int zSize, int sampleScale);
    }
}

#endif
