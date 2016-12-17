using Voxelmetric.Code.Common;

namespace Voxelmetric.Code.Utilities.Noise
{
    public class NoiseInterpolator
    {
        //! Downsampled size of chunk
        protected int m_size;
        protected int m_sizePow2;
        protected int m_sizePow3;
        //! +1 intepolated into downsampled state
        protected int m_step;
        //! Interpolated scale
        protected float m_scale;

        /// <summary>
        /// Initiates NoiseInterpolator
        /// </summary>
        /// <param name="downsamplingFactor">Factor says how much is ChunkSize supposed to be downsampled</param>
        public void SetInterpBitStep(int downsamplingFactor)
        {
            m_step = downsamplingFactor;
            m_size = (Env.ChunkSize>>m_step)+1;
            m_sizePow2 = m_size*m_size;
            m_sizePow3 = m_sizePow2*m_size;
            m_scale = 1f/(1<<m_step);
        }

        public int Step { get { return m_step;} }
        public int Size { get { return m_size; } }

        /// <summary>
        /// Interpolate chunk coordinates into downsampled coordinates of a lookuptable and returns and interpolated value from it
        /// </summary>
        /// <param name="x">Position within chunk on the x axis</param>
        /// <param name="z">Position within chunk on the z axis</param>
        /// <param name="lookupTable">Lookup table to be used to interpolate</param>
        public float Interpolate(int x, int z, float[] lookupTable)
        {
            float xs = (x + 0.5f) * m_scale;
            float zs = (z + 0.5f) * m_scale;

            int x0 = Helpers.FastFloor(xs);
            int z0 = Helpers.FastFloor(zs);

            xs = (xs - x0);
            zs = (zs - z0);

            int lookupIndex = Helpers.GetIndex1DFrom2D(x0, z0, m_size);
            int lookupIndex2 = lookupIndex+m_size; // x0,z0+1

            return Helpers.Interpolate(
                Helpers.Interpolate(lookupTable[lookupIndex], lookupTable[lookupIndex+1], xs),
                Helpers.Interpolate(lookupTable[lookupIndex2], lookupTable[lookupIndex2+1], xs),
                zs);
        }

        /// <summary>
        /// Interpolate chunk coordinates into downsampled coordinates of a lookuptable and returns and interpolated value from it
        /// </summary>
        /// <param name="x">Position within chunk on the x axis</param>
        /// <param name="y">Position within chunk on the y axis</param>
        /// <param name="z">Position within chunk on the z axis</param>
        /// <param name="lookupTable">Lookup table to be used to interpolate</param>
        public float Interpolate(int x, int y, int z, float[] lookupTable)
        {
            float xs = (x+0.5f)*m_scale;
            float ys = (y+0.5f)*m_scale;
            float zs = (z+0.5f)*m_scale;

            int x0 = Helpers.FastFloor(xs);
            int y0 = Helpers.FastFloor(ys);
            int z0 = Helpers.FastFloor(zs);

            xs = (xs-x0);
            ys = (ys-y0);
            zs = (zs-z0);

            int lookupIndex = Helpers.GetIndex1DFrom3D(x0, y0, z0, m_size, m_size);
            int lookupIndexY = lookupIndex+m_sizePow2; // x0, y0+1, z0
            int lookupIndexZ = lookupIndex+m_size;  // x0, y0, z0+1
            int lookupIndexYZ = lookupIndex+m_sizePow3; // x0, y0+1, z0+1

            return Helpers.Interpolate(
                Helpers.Interpolate(
                    Helpers.Interpolate(lookupTable[lookupIndex], lookupTable[lookupIndex+1], xs),
                    Helpers.Interpolate(lookupTable[lookupIndexY], lookupTable[lookupIndexY+1], xs),
                    ys),
                Helpers.Interpolate(
                    Helpers.Interpolate(lookupTable[lookupIndexZ], lookupTable[lookupIndexZ+1], xs),
                    Helpers.Interpolate(lookupTable[lookupIndexYZ], lookupTable[lookupIndexYZ+1], xs),
                    ys),
                zs);
        }
    }
}
