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

        public void SetInterpBitStep(int interpBitStep)
        {
            m_step = interpBitStep;
            m_size = (Env.ChunkSize>>m_step)+1;
            m_sizePow2 = m_size*m_size;
            m_sizePow3 = m_sizePow2*m_size;
            m_scale = 1f/(1<<m_step);
        }

        public int Step { get { return m_step;} }
        public int Size { get { return m_size; } }


        public float Interpolate(int localX, int localZ, float[] interpLookup)
        {
            float xs = (localX + 0.5f) * m_scale;
            float zs = (localZ + 0.5f) * m_scale;

            int x0 = Helpers.FastFloor(xs);
            int z0 = Helpers.FastFloor(zs);

            xs = (xs - x0);
            zs = (zs - z0);

            int lookupIndex = Helpers.GetIndex1DFrom2D(x0, z0, m_size);
            int lookupIndex2 = lookupIndex+m_size; // x0,z0+1

            try
            {
                return Helpers.Interpolate(
                    Helpers.Interpolate(interpLookup[lookupIndex], interpLookup[lookupIndex+1], xs),
                    Helpers.Interpolate(interpLookup[lookupIndex2], interpLookup[lookupIndex2+1], xs),
                    zs);
            }
            catch
            {
                return 0;
            }
        }

        public float Interpolate(int localX, int localY, int localZ, float[] interpLookup)
        {
            float xs = (localX+0.5f)*m_scale;
            float ys = (localY+0.5f)*m_scale;
            float zs = (localZ+0.5f)*m_scale;

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
                    Helpers.Interpolate(interpLookup[lookupIndex], interpLookup[lookupIndex+1], xs),
                    Helpers.Interpolate(interpLookup[lookupIndexY], interpLookup[lookupIndexY+1], xs),
                    ys),
                Helpers.Interpolate(
                    Helpers.Interpolate(interpLookup[lookupIndexZ], interpLookup[lookupIndexZ+1], xs),
                    Helpers.Interpolate(interpLookup[lookupIndexYZ], interpLookup[lookupIndexYZ+1], xs),
                    ys),
                zs);
        }
    }
}
