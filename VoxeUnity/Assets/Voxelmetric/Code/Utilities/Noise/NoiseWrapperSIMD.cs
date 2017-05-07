#if (UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN) && ENABLE_FASTSIMD
namespace Voxelmetric.Code.Utilities.Noise
{
    public class NoiseWrapperSIMD
    {
        protected readonly FastNoiseSIMD noise;

        public FastNoiseSIMD.NoiseType noiseType = FastNoiseSIMD.NoiseType.Simplex;
        public FastNoiseSIMD.FractalType fractalType = FastNoiseSIMD.FractalType.FBM;

        public float frequency = 0.01f;
        public int octaves = 3;
        public float lacunarity = 2.0f;
        public float gain = 0.5f;

        public NoiseWrapperSIMD(string seed)
        {
            noise = new FastNoiseSIMD(seed.GetHashCode());
            noise.SetNoiseType(noiseType);
            noise.SetFractalType(fractalType);
            noise.SetFrequency(frequency);
            noise.SetFractalOctaves(octaves);
            noise.SetFractalLacunarity(lacunarity);
            noise.SetFractalGain(gain);
        }

        public FastNoiseSIMD Noise
        {
            get { return noise; }
        }

        public FastNoiseSIMD.NoiseType NoiseType
        {
            set
            {
                noiseType = value;
                noise.SetNoiseType(noiseType);
            }
            get { return noiseType; }
        }

        public FastNoiseSIMD.FractalType FractalType
        {
            set
            {
                fractalType = value;
                noise.SetFractalType(fractalType);
            }
            get { return fractalType; }
        }

        public float Frequency
        {
            set
            {
                frequency = value;
                noise.SetFrequency(frequency);
            }
            get { return frequency; }
        }

        public int Octaves
        {
            set
            {
                octaves = value;
                noise.SetFractalOctaves(octaves);
            }
            get { return octaves; }
        }

        public float Lacuranity
        {
            set
            {
                lacunarity = value;
                noise.SetFractalLacunarity(lacunarity);
            }
            get { return lacunarity; }
        }

        public float Gain
        {
            set
            {
                gain = value;
                noise.SetFractalGain(gain);
            }
            get { return gain; }
        }
    }
}

#endif