namespace Voxelmetric.Code.Utilities.Noise
{
    public class NoiseWrapper
    {
        protected readonly FastNoise noise;

        public FastNoise.NoiseType noiseType = FastNoise.NoiseType.Simplex;
        public FastNoise.FractalType fractalType = FastNoise.FractalType.FBM;

        public float frequency = 0.01f;
        public int octaves = 3;
        public float lacunarity = 2.0f;
        public float gain = 0.5f;

        public NoiseWrapper(string seed)
        {
            noise = new FastNoise(seed.GetHashCode());
            noise.SetNoiseType(noiseType);
            noise.SetInterp(FastNoise.Interp.Quintic);
            noise.SetFractalType(fractalType);
            noise.SetFrequency(frequency);
            noise.SetFractalOctaves(octaves);
            noise.SetFractalLacunarity(lacunarity);
            noise.SetFractalGain(gain);
        }

        public FastNoise Noise
        {
            get { return noise; }
        }

        public FastNoise.NoiseType NoiseType
        {
            set
            {
                noiseType = value;
                noise.SetNoiseType(noiseType);
            }
            get { return noiseType; }
        }

        public FastNoise.FractalType FractalType
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