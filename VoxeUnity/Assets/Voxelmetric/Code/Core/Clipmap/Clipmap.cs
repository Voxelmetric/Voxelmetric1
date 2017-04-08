using Voxelmetric.Code.Common;

namespace Voxelmetric.Code.Core.Clipmap
{
    public class Clipmap
    {
        private readonly AxisInfo[] m_axes;

        public int VisibleRange { get; private set; }
        public int RangeYMin { get; private set; }
        public int RangeYMax { get; private set; }

        public Clipmap(int visibleRange, int rangeYMin, int rangeYMax)
        {
            VisibleRange = visibleRange;

            if (rangeYMin < -visibleRange)
                rangeYMin = -visibleRange;
            if (rangeYMax > visibleRange)
                rangeYMax = visibleRange;

            RangeYMin = rangeYMin;
            RangeYMax = rangeYMax;

            m_axes = new[]
            {
                new AxisInfo
                {
                    Map = new ClipmapItem[2*visibleRange+1],
                    Offset = 0,
                    RangeMin = -visibleRange,
                    RangeMax = visibleRange
                },
                new AxisInfo
                {
                    Map = new ClipmapItem[2*visibleRange+1],
                    Offset = 0,
                    RangeMin = -rangeYMin,
                    RangeMax = rangeYMax
                },
                new AxisInfo
                {
                    Map = new ClipmapItem[2*visibleRange+1],
                    Offset = 0,
                    RangeMin = -visibleRange,
                    RangeMax = visibleRange
                }
            };
        }

        public ClipmapItem this[int x, int y, int z]
        {
            get
            {
                int tx = TransformX(x);
                int ty = TransformY(y);
                int tz = TransformZ(z);
                return Get_Transformed(tx, ty, tz);
            }
        }

        public ClipmapItem Get_Transformed(int tx, int ty, int tz)
        {
            // Clamp coordinates to the array range
            int xx = tx.Clamp(0, m_axes[0].Map.Length - 1);
            int yy = ty.Clamp(0, m_axes[1].Map.Length - 1);
            int zz = tz.Clamp(0, m_axes[2].Map.Length - 1);

            // Pick the furthest one
            int absX = Helpers.Abs(xx);
            int absY = Helpers.Abs(yy);
            int absZ = Helpers.Abs(zz);

            /*if (absX > absZ)
                return (absX > absY) ? m_axes[0].Map[xx] : m_axes[1].Map[yy];

            return absZ > absY ? m_axes[2].Map[zz] : m_axes[1].Map[yy];*/
            int index = 0;
            int value = xx;

            if (absY>absX && absY>absZ)
            {
                index = 1;
                value = yy;
            }
            else if (absZ>absX && absZ>absY)
            {
                index = 2;
                value = zz;
            }

            return m_axes[index].Map[value];
        }

        private void InitAxis(int axis, int forceLOD, float coefLOD)
        {
            var axisInfo = m_axes[axis];
            for (int distance = axisInfo.RangeMin; distance<=axisInfo.RangeMax; distance++)
            {
                int lod = DetermineLOD(distance, forceLOD, coefLOD);
                bool isInVisibilityRange = IsInVisibilityRange(axisInfo, distance);

                axisInfo.Map[distance+VisibleRange] = new ClipmapItem
                {
                    LOD = lod,
                    IsInVisibleRange = isInVisibilityRange
                };
            }
        }

        public void Init(int forceLOD, float coefLOD)
        {
            for (int axis = 0; axis<3; axis++)
                InitAxis(axis, forceLOD, coefLOD);
        }

        public void SetOffset(int x, int y, int z)
        {
            m_axes[0].Offset = -x;
            m_axes[1].Offset = -y;
            m_axes[2].Offset = -z;
        }

        public int TransformX(int x)
        {
            return
            // Adjust the coordinate depending on the offset
            x + m_axes[0].Offset
            // Center them out
            + VisibleRange;
        }

        public int TransformY(int y)
        {
            return
            // Adjust the coordinate depending on the offset
            y + m_axes[1].Offset
            // Center them out
            + VisibleRange;
        }

        public int TransformZ(int z)
        {
            return
            // Adjust the coordinate depending on the offset
            z + m_axes[2].Offset
            // Center them out
            + VisibleRange;
        }

        public bool IsInsideBounds_Transformed(int tx, int ty, int tz)
        {
            // Clamp coordinates to the array range
            return tx>=0 && ty>=0 && tz>=00 &&
                   tx<m_axes[0].Map.Length &&
                   ty<m_axes[1].Map.Length &&
                   tz<m_axes[2].Map.Length;
        }

        private static int DetermineLOD(int distance, int forceLOD, float coefLOD)
        {
            int lod = 0;

            if (forceLOD >= 0)
            {
                lod = forceLOD;
            }
            else
            {
                if (coefLOD <= 0)
                    return 0;

                // Pick the greater distance and choose a proper LOD
                int dist = Helpers.Abs(distance);
                lod = (int)(dist / (coefLOD * Env.ChunkPow));
            }

            // LOD can't be bigger than chunk size
            if (lod < 0)
                lod = 0;
            if (lod > Env.ChunkPow)
                lod = Env.ChunkPow;

            return lod;
        }

        private bool IsInVisibilityRange(AxisInfo axis, int distance)
        {
            return distance>=axis.RangeMin && distance<=axis.RangeMax;
        }

        private class AxisInfo
        {
            public ClipmapItem[] Map; // -N ... 0 ... N
            public int Offset;
            public int RangeMax;
            public int RangeMin;
        }
    }
}