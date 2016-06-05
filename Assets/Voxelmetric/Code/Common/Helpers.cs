namespace Assets.Voxelmetric.Code.Common
{
    public static class Helpers
    {
        public static int GetIndex1DFrom2D(int x, int z, int sizeX)
        {
            return x + z * sizeX;
        }
        
        public static int GetIndex1DFrom3D(int x, int y, int z, int sizeX, int sizeZ)
        {
            return x + sizeX * (z + y * sizeZ);
        }

        public static int GetChunkIndex1DFrom3D(int x, int y, int z)
        {
            return x + (z << Config.Env.ChunkPower) + (y << Config.Env.ChunkPower2);
        }

        public static void GetIndex2DFrom1D(int index, out int x, out int z, int sizeX)
        {
            x = index % sizeX;
            z = index / sizeX;
        }

        public static void GetIndex3DFrom1D(int index, out int x, out int y, out int z, int sizeX, int sizeZ)
        {
            x = index % sizeX;
            y = index / (sizeX * sizeZ);
            z = (index / sizeX) % sizeZ;
        }

        public static void GetChunkIndex3DFrom1D(int index, out int x, out int y, out int z)
        {
            x = index & Config.Env.ChunkMask;
            y = index >> Config.Env.ChunkPower2;
            z = (index >> Config.Env.ChunkPower) & Config.Env.ChunkMask;
        }

        public static T[] CreateArray1D<T>(int size)
        {
            return new T[size];
        }

        public static T[] CreateAndInitArray1D<T>(int size)
        {
            var arr = new T[size];
            for (int i = 0; i < arr.Length; i++)
                arr[i] = default(T);

            return arr;
        }

        public static T[][] CreateArray2D<T>(int sizeX, int sizeY)
        {
            var arr = new T[sizeX][];

            for (int i = 0; i < arr.Length; i++)
                arr[i] = new T[sizeY];

            return arr;
        }

        public static T[][] CreateAndInitArray2D<T>(int sizeX, int sizeY)
        {
            var arr = new T[sizeX][];

            for (int i = 0; i < arr.Length; i++)
            {
                arr[i] = new T[sizeY];
                for (int j = 0; j < arr[i].Length; j++)
                    arr[i][j] = default(T);
            }

            return arr;
        }

        public static float Interpolate(float x0, float x1, float alpha)
        {
            return x0 + (x1 - x0) * alpha;
        }

        // Finds the smallest positive t such that s+t*ds is an integer
        public static float IntBound(float s, float ds)
        {
            /* Recursive version                
			    if (ds < 0)
                {
				    return IntBound(-s, -ds);
                }
			    else
			    {
				    s = Mod(s, 1);
                    // Problem is now s+t*ds = 1
				    return (1 - s) / ds;
			    }
             */
            while (true)
            {
                if (ds < 0)
                {
                    s = -s;
                    ds = -ds;
                    continue;
                }

                s = Mod(s, 1);
                return (1 - s) / ds;
            }
        }

        public static int SigNum(float x)
        {
            return (x > 0) ? 1 : ((x < 0) ? -1 : 0);
        }

        public static int SigShift(int value, int shift)
        {
            return (shift > 0) ? value << shift : value >> shift;
        }

        public static int FastFloor(float val)
        {
            return (val > 0) ? (int)val : (int)val - 1;
        }

        // Custom modulo. Handles negative numbers.
        public static int Mod(int value, int modulus)
        {
            int r = value % modulus;
            return (r < 0) ? (r + modulus) : r;
        }

        public static float Mod(float value, int modulus)
        {
            return (value % modulus + modulus) % modulus;
        }

        public static float Clamp(this float val, float min, float max)
        {
            if (val < min)
                return min;

            return val > 0 ? max : val;
        }
    }
}
