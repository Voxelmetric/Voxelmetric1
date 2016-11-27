using System.Threading;
using Voxelmetric.Code.Utilities;

namespace Voxelmetric.Code.Common
{
	public static class Helpers
	{
	    public static int MainThreadID = Thread.CurrentThread.ManagedThreadId;

	    public static bool IsMainThread
	    {
	        get { return Thread.CurrentThread.ManagedThreadId==MainThreadID; }
	    }

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
            /*
			 In the past, indexs were computed using:
			 x + (z << Env.ChunkPower) + (y << Env.ChunkPower2);
			 However, since then padding was introduced and real size might no longer be a power of 2
			*/
            return x+Env.ChunkPadding + Env.ChunkSizeWithPadding * ((z+Env.ChunkPadding) + (y+Env.ChunkPadding) * Env.ChunkSizeWithPadding);
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
	        /*
			 In the past, indexs were computed using:
			 x = index & Env.ChunkMask;
			 y = index >> Env.ChunkPower2;
			 z = (index >> Env.ChunkPower) & Env.ChunkMask;
			 However, since then padding was introduced and real size might no longer be a power of 2
			*/
	        x = index % Env.ChunkSizeWithPadding;
	        y = index / Env.ChunkSizeWithPaddingPow2;
	        z = (index / Env.ChunkSizeWithPadding) % Env.ChunkSizeWithPadding;

	        x -= Env.ChunkPadding;
	        y -= Env.ChunkPadding;
	        z -= Env.ChunkPadding;
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

		public static uint Mod3(uint value)
		{
			uint a = value&0x33333333; /* even two-bit groups */
			uint b = value&0xcccccccc; /* odd two-bit groups */
			uint sum = a+(b>>2); /* sum 0-6 in 8 groups */
			sum = sum+(sum>>2); /* sum 0-3 in 8 groups */
			sum = sum&0x33333333; /* clear garbage bits */
			sum = sum+(sum>>4); /* sum 0-6 in 4 groups */
			sum = sum+(sum>>2); /* sum 0-3 in 4 groups */
			sum = sum&0x33333333; /* clear garbage bits */
			sum = sum+(sum>>8); /* sum 0-6 in 2 groups */
			sum = sum+(sum>>2); /* sum 0-3 in 2 groups */
			sum = sum&0x33333333; /* clear garbage bits */
			sum = sum+(sum>>16); /* sum 0-6 in 1 group */
			sum = sum+(sum>>2); /* sum 0-3 in 1 group */
			sum = sum&0x3; /* clear garbage bits */
			return sum;
		}

		public static int Clamp(this int val, int min, int max)
		{
			if (val < min)
				return min;

			return val > max ? max : val;
		}

		public static float Clamp(this float val, float min, float max)
		{
			if (val < min)
				return min;

			return val > max ? max : val;
		}

		public static int Abs(int val)
		{
			return (val + (val >> 31)) ^ (val >> 31);
		}
	}
}
